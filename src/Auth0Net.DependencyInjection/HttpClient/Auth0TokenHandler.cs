using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Auth0Net.DependencyInjection.Cache;
using Microsoft.Extensions.Options;

namespace Auth0Net.DependencyInjection.HttpClient
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> that adds a authentication header with a Auth0-generated JWT token for the given audience.
    /// </summary>
    public class Auth0TokenHandler : DelegatingHandler
    {
        private const string Scheme = "Bearer";
        private readonly IAuth0TokenCache _cache;
        private readonly Auth0TokenHandlerConfig _handlerConfig;

        /// <summary>
        /// Constructs a new instance of the <see cref="Auth0TokenHandler"/>
        /// </summary>
        /// <param name="cache">An instance of an <see cref="IAuth0TokenCache"/>.</param>
        /// <param name="handlerConfig">The configuration for this handler.</param>
        public Auth0TokenHandler(IAuth0TokenCache cache, Auth0TokenHandlerConfig handlerConfig)
        {
            _cache = cache;
            _handlerConfig = handlerConfig;
        }

        /// <inheritdoc cref="DelegatingHandler"/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var audience = _handlerConfig.Audience ?? _handlerConfig.AudienceResolver?.Invoke(request) ?? throw new ArgumentException("Audience cannot be computed");

            request.Headers.Authorization = new AuthenticationHeaderValue(Scheme, await _cache.GetTokenAsync(audience));
            return await base.SendAsync(request, cancellationToken);
        }
    }

    internal class Auth0ManagementTokenHandler : Auth0TokenHandler
    {
        public Auth0ManagementTokenHandler(IAuth0TokenCache cache, IOptionsSnapshot<Auth0Configuration> options, Auth0ManagementTokenConfiguration clientConfig) 
            : base(cache, new Auth0TokenHandlerConfig(UriHelpers.GetValidManagementUri(clientConfig.AudienceDomainOverride ?? options.Value.Domain).ToString()))
        {
        }
    }
}
