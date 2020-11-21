using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Auth0Net.DependencyInjection.Cache;

namespace Auth0Net.DependencyInjection.HttpClient
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> that adds a authentication header with a Auth0-generated JWT token for the given audience.
    /// </summary>
    public class Auth0TokenHandler : DelegatingHandler
    {
        private const string Scheme = "Bearer";
        private readonly IAuth0TokenCache _cache;
        private readonly Auth0TokenConfig _config;

        /// <summary>
        /// Constructs a new instance of the <see cref="Auth0TokenHandler"/>
        /// </summary>
        /// <param name="cache">An instance of an <see cref="IAuth0TokenCache"/>.</param>
        /// <param name="config">The configuration for this handler.</param>
        public Auth0TokenHandler(IAuth0TokenCache cache, Auth0TokenConfig config)
        {
            _cache = cache;
            _config = config;
        }

        /// <inheritdoc cref="DelegatingHandler"/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var audience = _config.Audience ?? request.RequestUri?.AbsoluteUri ?? throw new ArgumentException("Audience cannot be computed");

            request.Headers.Authorization = new AuthenticationHeaderValue(Scheme, await _cache.GetTokenAsync(audience));
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
