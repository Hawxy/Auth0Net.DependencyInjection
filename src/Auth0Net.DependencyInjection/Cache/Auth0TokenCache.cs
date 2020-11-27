using System;
using System.Threading.Tasks;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0Net.DependencyInjection.HttpClient;
using LazyCache;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth0Net.DependencyInjection.Cache
{        
    /// <inheritdoc cref="IAuth0TokenCache"/>
    public class Auth0TokenCache : IAuth0TokenCache
    {
        private readonly AuthenticationApiClient _client;
        private readonly IAppCache _cache;
        private readonly ILogger<Auth0TokenCache> _logger;
        private readonly Auth0Configuration _config;

        /// <summary>
        /// An implementation of <see cref="IAuth0TokenCache"/> that caches Auth0 Access Tokens until 5 minutes before expiry
        /// </summary>
        /// <param name="client">The Authentication Client</param>
        /// <param name="cache">An application cache from LazyCache </param>
        /// <param name="logger"></param>
        /// <param name="config"></param>
        public Auth0TokenCache(AuthenticationApiClient client, IAppCache cache, ILogger<Auth0TokenCache> logger, IOptionsSnapshot<Auth0Configuration> config)
        {
            _client = client;
            _cache = cache;
            _logger = logger;
            _config = config.Value;
        }

        /// <inheritdoc cref="IAuth0TokenCache"/>
        public async Task<string> GetTokenAsync(string audience)
        {
            return await _cache.GetOrAddAsync($"{nameof(Auth0TokenCache)}-{audience}", async e =>
            {
                _logger.LogDebug("Auth0 Token fetch was requested for audience {audience}", audience);
                var tokenRequest = new ClientCredentialsTokenRequest
                {
                    ClientId = _config.ClientId,
                    ClientSecret = _config.ClientSecret,
                    Audience = audience
                };

                var response = await _client.GetTokenAsync(tokenRequest);

                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(response.ExpiresIn).Subtract(_config.RenewTokenAt);
                return response.AccessToken;
            });
        }

        /// <inheritdoc cref="IAuth0TokenCache"/>
        public async Task<string> GetTokenAsync(Uri audience) => await GetTokenAsync(audience.ToString());

        /// <inheritdoc cref="IAuth0TokenCache"/>
        public async Task<string> GetManagementTokenAsync() => await GetTokenAsync(UriHelpers.GetValidManagementUri(_config.Domain));
    }

}
