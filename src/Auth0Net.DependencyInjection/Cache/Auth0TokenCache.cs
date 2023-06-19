using System.Diagnostics;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0Net.DependencyInjection.HttpClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace Auth0Net.DependencyInjection.Cache;

/// <inheritdoc cref="IAuth0TokenCache"/>
public sealed class Auth0TokenCache : IAuth0TokenCache
{
    private readonly IAuthenticationApiClient _client;
    private readonly IFusionCache _cache;
    private readonly ILogger<Auth0TokenCache> _logger;
    private readonly Auth0Configuration _config;

    private const double TokenExpiryBuffer = 0.01d;
    
    private static string Key(string audience) => $"{nameof(Auth0TokenCache)}-{audience}";

    /// <summary>
    /// An implementation of <see cref="IAuth0TokenCache"/> that caches and renews Auth0 Access Tokens
    /// </summary>
    public Auth0TokenCache(IAuthenticationApiClient client, IFusionCacheProvider provider, ILogger<Auth0TokenCache> logger, IOptions<Auth0Configuration> config)
    {
        _client = client;
        _cache = provider.GetCache(Constants.FusionCacheInstance);
        _logger = logger;
        _config = config.Value;
    }

    /// <inheritdoc cref="IAuth0TokenCache"/>
    public async ValueTask<string> GetTokenAsync(string audience, CancellationToken token = default)
    {
        _logger.TokenRequested(audience);

        return (await _cache.GetOrSetAsync<string>(Key(audience), async (config, ct) =>
        {
            _logger.CacheMiss(audience);

            var tokenRequest = new ClientCredentialsTokenRequest
            {
                ClientId = _config.ClientId,
                ClientSecret = _config.ClientSecret,
                Audience = audience
            };

            var response = await _client.GetTokenAsync(tokenRequest, ct);

            var computedExpiry = Math.Ceiling(response.ExpiresIn - response.ExpiresIn * TokenExpiryBuffer);
            Debug.Assert(computedExpiry > 0);
            
            var expiry = TimeSpan.FromSeconds(computedExpiry);
            _logger.ExpiresAt(audience, expiry);

            config.Options.SetDuration(expiry);
            config.Options.SetEagerRefresh(0.95f);

            return response.AccessToken;
        }, token: token))!;
    }

    /// <inheritdoc cref="IAuth0TokenCache"/>
    public ValueTask<string> GetTokenAsync(Uri audience, CancellationToken token = default) => GetTokenAsync(audience.ToString(), token);

    /// <inheritdoc cref="IAuth0TokenCache"/>
    public ValueTask<string> GetManagementTokenAsync(CancellationToken token = default) => GetTokenAsync(UriHelpers.GetValidManagementUri(_config.Domain), token);
}

internal static partial class Log
{
    [LoggerMessage(Message = "Auth0 Token was requested for audience {audience}", Level = LogLevel.Trace)]
    public static partial void TokenRequested(this ILogger logger, string audience);
    
    [LoggerMessage(Message = "Auth0 Token cache missed, fetching new token for audience {audience}", Level = LogLevel.Trace)]
    public static partial void CacheMiss(this ILogger logger, string audience);
    
    [LoggerMessage(Message = "Auth0 Token for audience {audience} will expire at {expiry}", Level = LogLevel.Trace)]
    public static partial void ExpiresAt(this ILogger logger, string audience, TimeSpan expiry);
}