using System.Net.Http;
using System.Net.Http.Headers;
using Auth0Net.DependencyInjection.Cache;
using Microsoft.Extensions.Options;

namespace Auth0Net.DependencyInjection.HttpClient;

internal sealed class Auth0ManagementTokenHandler : DelegatingHandler
{
    private const string Scheme = "Bearer";
    private readonly IAuth0TokenCache _cache;
    private readonly IOptions<Auth0Configuration> _auth0Configuration;
    private readonly Auth0ManagementTokenConfiguration _managementConfiguration;

    public Auth0ManagementTokenHandler(IAuth0TokenCache cache, IOptions<Auth0Configuration> auth0Configuration,  Auth0ManagementTokenConfiguration managementConfiguration)
    {
        _cache = cache;
        _auth0Configuration = auth0Configuration;
        _managementConfiguration = managementConfiguration;
    }

    /// <inheritdoc cref="DelegatingHandler"/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _cache.GetTokenAsync(_managementConfiguration.Audience ?? _auth0Configuration.Value.Domain, cancellationToken);
        
        request.Headers.Authorization = new AuthenticationHeaderValue(Scheme, token);
        
        return await base.SendAsync(request, cancellationToken);
    }
}