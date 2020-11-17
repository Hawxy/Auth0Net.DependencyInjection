using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Auth0NET.DependencyInjection.Cache;

namespace Auth0NET.DependencyInjection.HttpClient
{
    public class Auth0TokenHandler : DelegatingHandler
    {
        private const string Scheme = "Bearer";
        private readonly IAuth0TokenCache _cache;
        private readonly Auth0TokenConfig _config;
        public Auth0TokenHandler(IAuth0TokenCache cache, Auth0TokenConfig config)
        {
            _cache = cache;
            _config = config;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(Scheme, await _cache.GetTokenAsync(_config.Audience));
            return await base.SendAsync(request, cancellationToken);
        }
    }

    public class Auth0TokenConfig
    {
        public string Audience { get; set; } = null!;

        public Auth0TokenConfig() { }

        public Auth0TokenConfig(string audience)
        {
            Audience = audience;
        }
    }
}
