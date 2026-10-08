using System.Security.Cryptography;
using System.Text;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using ZiggyCreatures.Caching.Fusion;

namespace Auth0Net.DependencyInjection.Cache;

/// <inheritdoc cref="IAuth0OnBehalfOfTokenCache"/>
public sealed class Auth0OnBehalfOfTokenCache : IAuth0OnBehalfOfTokenCache
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    private readonly IAuthenticationApiClient _client;
    private readonly IFusionCache _cache;
    private readonly ILogger<Auth0OnBehalfOfTokenCache> _logger;
    private readonly Auth0OnBehalfOfConfiguration _config;

    private const double TokenExpiryBuffer = 0.01d;

    /// <summary>
    /// An implementation of <see cref="IAuth0OnBehalfOfTokenCache"/> that exchanges and caches Auth0 On-Behalf-Of access tokens.
    /// </summary>
    public Auth0OnBehalfOfTokenCache(IAuthenticationApiClient client, IFusionCacheProvider provider, ILogger<Auth0OnBehalfOfTokenCache> logger,
        IOptions<Auth0Configuration> config, IOptions<Auth0OnBehalfOfConfiguration> onBehalfOfConfig)
    {
        _client = client;

        var cache = config.Value.FusionCacheResolver != null ? config.Value.FusionCacheResolver(provider) : provider.GetCache(Constants.FusionCacheInstance);

        _cache = cache ?? throw new InvalidOperationException($"Unable to resolve requested FusionCache instance. Something has gone very wrong.");
        _logger = logger;
        _config = onBehalfOfConfig.Value;
    }

    /// <inheritdoc cref="IAuth0OnBehalfOfTokenCache.GetTokenAsync"/>
    public async ValueTask<OnBehalfOfToken> GetTokenAsync(string subjectToken, string audience, string? scope = null, string? organization = null, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(subjectToken))
            throw new ArgumentException("Subject token cannot be null or empty.", nameof(subjectToken));
        if (string.IsNullOrWhiteSpace(audience))
            throw new ArgumentException("Audience cannot be null or empty.", nameof(audience));

        _logger.OnBehalfOfTokenRequested(audience);

        var subjectExpiry = ReadSubjectExpiry(subjectToken);
        if (subjectExpiry <= DateTimeOffset.UtcNow)
        {
            var expired = Auth0OnBehalfOfException.SubjectTokenExpired();
            _logger.OnBehalfOfExchangeFailed(audience, expired.StatusCode, expired.Error);
            throw expired;
        }

        var normalizedScope = NormalizeScope(scope);

        return (await _cache.GetOrSetAsync<OnBehalfOfToken>(Key(subjectToken, audience, normalizedScope, organization), async (ctx, ct) =>
        {
            var request = new OnBehalfOfTokenRequest
            {
                SubjectToken = subjectToken,
                Audience = audience,
                Scope = normalizedScope.Length > 0 ? normalizedScope : null!,
                Organization = string.IsNullOrEmpty(organization) ? null! : organization!,
                ClientId = _config.ClientId!,
                ClientSecret = _config.ClientSecret!,
                ClientAssertionSecurityKey = _config.ClientAssertionSecurityKey!,
                ClientAssertionSecurityKeyAlgorithm = _config.ClientAssertionSecurityKeyAlgorithm!
            };

            OnBehalfOfTokenResponse response;
            try
            {
                response = await _client.GetTokenOnBehalfOfAsync(request, ct);
            }
            catch (RateLimitApiException ex)
            {
                throw Failed(audience, Auth0OnBehalfOfException.From(ex));
            }
            catch (ErrorApiException ex)
            {
                throw Failed(audience, Auth0OnBehalfOfException.From(ex));
            }

            var issuedAt = DateTimeOffset.UtcNow;

            var duration = TimeSpan.FromSeconds(Math.Ceiling(response.ExpiresIn - response.ExpiresIn * TokenExpiryBuffer));
            if (subjectExpiry is { } expiry && expiry - issuedAt < duration)
                duration = expiry - issuedAt;

            if (duration > TimeSpan.Zero)
            {
                ctx.Options.Duration = duration;
            }
            else
            {
                // The subject token expired during the exchange.
                ctx.Options.SkipMemoryCacheWrite = true;
                ctx.Options.SkipDistributedCacheWrite = true;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.OnBehalfOfTokenIssued(audience, response.GetCurrentActor(), Math.Max(duration.TotalSeconds, 0));

            return new OnBehalfOfToken(response.AccessToken, issuedAt.AddSeconds(response.ExpiresIn), response.Scope);
        }, options =>
        {
            // Entries are tied to one user's token and must never outlive it.
            options.IsFailSafeEnabled = false;
            options.AllowStaleOnReadOnly = false;
            options.EagerRefreshThreshold = null;
            options.JitterMaxDuration = TimeSpan.Zero;
            options.MemoryCacheDuration = null;
            options.DistributedCacheDuration = null;
        }, token))!;
    }

    private Auth0OnBehalfOfException Failed(string audience, Auth0OnBehalfOfException exception)
    {
        _logger.OnBehalfOfExchangeFailed(audience, exception.StatusCode, exception.Error);
        return exception;
    }

    internal static string Key(string subjectToken, string audience, string normalizedScope, string? organization) =>
        $"{nameof(Auth0OnBehalfOfTokenCache)}:{Hash($"{subjectToken}\0{audience}\0{normalizedScope}\0{organization ?? ""}")}";

    internal static string NormalizeScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
            return string.Empty;

        var scopes = scope!
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal);

        return string.Join(" ", scopes);
    }

    /// <summary>
    /// Returns the base64url-encoded SHA-256 hash of <paramref name="value"/>.
    /// </summary>
    private static string Hash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
#if NET8_0_OR_GREATER
        var hash = SHA256.HashData(bytes);
#else
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
#endif
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// Reads the subject token's exp claim without validating the token. Returns null for opaque tokens or tokens without exp.
    /// </summary>
    private static DateTimeOffset? ReadSubjectExpiry(string subjectToken)
    {
        if (!TokenHandler.CanReadToken(subjectToken))
            return null;

        try
        {
            var validTo = TokenHandler.ReadJsonWebToken(subjectToken).ValidTo;
            return validTo == DateTime.MinValue ? null : new DateTimeOffset(DateTime.SpecifyKind(validTo, DateTimeKind.Utc));
        }
        catch (Exception)
        {
            return null;
        }
    }
}
