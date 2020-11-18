using System;
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
            var audience = _config.Audience ?? request.RequestUri?.AbsoluteUri ?? throw new ArgumentException("Audience cannot be computed");

            request.Headers.Authorization = new AuthenticationHeaderValue(Scheme, await _cache.GetTokenAsync(audience));
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
