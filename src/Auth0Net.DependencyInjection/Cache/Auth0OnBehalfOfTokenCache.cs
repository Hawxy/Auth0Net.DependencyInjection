#if NET9_0_OR_GREATER
using System.Buffers.Text;
#endif
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
    private readonly FusionCacheEntryOptions _entryOptions;
    private readonly ILogger<Auth0OnBehalfOfTokenCache> _logger;
    private readonly Auth0OnBehalfOfConfiguration _config;

    private const double TokenExpiryBuffer = 0.01d;
    private const double MinimumExpiryBufferSeconds = 30d;

    /// <summary>
    /// An implementation of <see cref="IAuth0OnBehalfOfTokenCache"/> that exchanges and caches Auth0 On-Behalf-Of access tokens.
    /// </summary>
    public Auth0OnBehalfOfTokenCache(IAuthenticationApiClient client, IFusionCacheProvider provider, ILogger<Auth0OnBehalfOfTokenCache> logger,
        IOptions<Auth0Configuration> config, IOptions<Auth0OnBehalfOfConfiguration> onBehalfOfConfig)
    {
        _client = client;
        _logger = logger;
        _config = onBehalfOfConfig.Value;

        var resolver = _config.FusionCacheResolver ?? config.Value.FusionCacheResolver;
        var cache = resolver != null ? resolver(provider) : provider.GetCache(Constants.FusionCacheInstance);

        _cache = cache ?? throw new InvalidOperationException($"Unable to resolve requested FusionCache instance. Something has gone very wrong.");

        _entryOptions = _cache.CreateEntryOptions(options =>
        {
            // Entries are tied to one user's token and must never outlive it.
            options.IsFailSafeEnabled = false;
            options.AllowStaleOnReadOnly = false;
            options.EagerRefreshThreshold = null;
            options.JitterMaxDuration = TimeSpan.Zero;
            options.MemoryCacheDuration = null;
            options.DistributedCacheDuration = null;
            // Entries never change once written, so other nodes don't need to be notified.
            options.SkipBackplaneNotifications = true;
            options.SkipDistributedCacheRead = !_config.UseDistributedCache;
            options.SkipDistributedCacheWrite = !_config.UseDistributedCache;
        });
    }

    /// <inheritdoc cref="IAuth0OnBehalfOfTokenCache.GetTokenAsync"/>
    public async ValueTask<OnBehalfOfToken> GetTokenAsync(string subjectToken, string audience, string? scope = null, string? organization = null, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(subjectToken))
            throw new ArgumentException("Subject token cannot be null or empty.", nameof(subjectToken));
        if (string.IsNullOrWhiteSpace(audience))
            throw new ArgumentException("Audience cannot be null or empty.", nameof(audience));

        _logger.OnBehalfOfTokenRequested(audience);

        var normalizedScope = NormalizeScope(scope);

        return (await _cache.GetOrSetAsync<OnBehalfOfToken>(Key(subjectToken, audience, normalizedScope, organization), async (ctx, ct) =>
        {
            // Only checked on a miss, as a cached entry never outlives the subject token.
            var subjectExpiry = ReadSubjectExpiry(subjectToken);
            if (subjectExpiry <= DateTimeOffset.UtcNow)
                throw Failed(audience, Auth0OnBehalfOfException.SubjectTokenExpired());

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

            var requestedAt = DateTimeOffset.UtcNow;

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

            var duration = CacheDuration(response.ExpiresIn, requestedAt, DateTimeOffset.UtcNow, subjectExpiry);

            if (duration > TimeSpan.Zero)
            {
                ctx.Options.Duration = duration;
            }
            else
            {
                ctx.Options.SkipMemoryCacheWrite = true;
                ctx.Options.SkipDistributedCacheWrite = true;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.OnBehalfOfTokenIssued(audience, response.GetCurrentActor(), Math.Max(duration.TotalSeconds, 0));

            return new OnBehalfOfToken(response.AccessToken, requestedAt.AddSeconds(response.ExpiresIn), response.Scope);
        }, _entryOptions, token))!;
    }

    private Auth0OnBehalfOfException Failed(string audience, Auth0OnBehalfOfException exception)
    {
        // A rejected or expired subject token is routine, so it is not logged above Debug.
        var level = exception.StatusCode == 401 ? LogLevel.Debug : LogLevel.Information;
        _logger.OnBehalfOfExchangeFailed(level, audience, exception.StatusCode, exception.Error);
        return exception;
    }

    /// <summary>
    /// Returns how long, from <paramref name="now"/>, an exchanged token can be cached.
    /// </summary>
    /// <remarks>
    /// The token is cached until its expiry minus 1% of its lifetime or 30 seconds, whichever is larger but at most half its lifetime,
    /// counted from when it was requested. The result is capped at <paramref name="subjectExpiry"/>, and is zero or negative if the token should not be cached.
    /// </remarks>
    internal static TimeSpan CacheDuration(double expiresIn, DateTimeOffset requestedAt, DateTimeOffset now, DateTimeOffset? subjectExpiry)
    {
        var buffer = Math.Min(Math.Max(expiresIn * TokenExpiryBuffer, MinimumExpiryBufferSeconds), expiresIn / 2);
        var cacheUntil = requestedAt.AddSeconds(expiresIn - buffer);

        if (subjectExpiry < cacheUntil)
            cacheUntil = subjectExpiry.Value;

        return cacheUntil - now;
    }

    internal static string Key(string subjectToken, string audience, string normalizedScope, string? organization) =>
        $"{nameof(Auth0OnBehalfOfTokenCache)}:{Hash($"{subjectToken}\0{audience}\0{normalizedScope}\0{organization ?? ""}")}";

    internal static string NormalizeScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
            return string.Empty;

        // A single scope needs no normalization.
        if (!scope!.Any(char.IsWhiteSpace))
            return scope;

        var scopes = scope
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
#if NET9_0_OR_GREATER
        return Base64Url.EncodeToString(SHA256.HashData(bytes));
#elif NET8_0_OR_GREATER
        return Convert.ToBase64String(SHA256.HashData(bytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
#else
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(bytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
#endif
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
