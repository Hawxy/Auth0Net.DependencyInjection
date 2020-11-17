using System;
using System.Threading.Tasks;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0Net.DependencyInjection.HttpClient;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth0NET.DependencyInjection.Cache
{
    public class Auth0TokenCache : IAuth0TokenCache
    {
        private const string ManagementCacheKey = "Management_Token";
        private readonly AuthenticationApiClient _client;
        private readonly IAppCache _cache;
        private readonly ILogger<Auth0TokenCache> _logger;
        private readonly Auth0TokenCacheConfiguration _config;

        public Auth0TokenCache(AuthenticationApiClient client, IAppCache cache, ILogger<Auth0TokenCache> logger, IOptionsSnapshot<Auth0TokenCacheConfiguration> config)
        {
            _client = client;
            _cache = cache;
            _logger = logger;
            _config = config.Value;
        }

        public async Task<string> GetTokenAsync(string audience)
        {
            var entry = await _cache.GetOrAddAsync($"{audience}_token", async e =>
            {
                _logger.LogDebug("Auth0 Token fetch was requested for audience {audience}", audience);
                var tokenRequest = new ClientCredentialsTokenRequest
                {
                    ClientId = _config.ClientId,
                    ClientSecret = _config.ClientSecret,
                    Audience = audience
                };

                var response = await _client.GetTokenAsync(tokenRequest);

                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(response.ExpiresIn).Subtract(TimeSpan.FromMinutes(5));
                return response.AccessToken;
            });

            return entry;
        }

        public async Task<string> GetTokenAsync(Uri audience) => await GetTokenAsync(audience.ToString());

        public async Task<string> GetManagementTokenAsync() => await GetTokenAsync(Auth0Util.GetAuth0ApiUrl(_config.Domain));
    }

}
