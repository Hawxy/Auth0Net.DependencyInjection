using System.Net.Http;
using System.Net.Http.Headers;
using Auth0Net.DependencyInjection.Cache;
using Auth0Net.DependencyInjection.Organizations;

namespace Auth0Net.DependencyInjection.HttpClient;

/// <summary>
/// A <see cref="DelegatingHandler"/> that adds an authentication header with an Auth0-generated JWT token for the given audience.
/// </summary>
public class Auth0TokenHandler : DelegatingHandler
{
    private const string Scheme = "Bearer";
    private readonly IAuth0TokenCache _cache;
    private readonly Auth0TokenHandlerConfig _handlerConfig;
    private readonly HttpClientOrganizationAccessor _accessor;

    /// <summary>
    /// Constructs a new instance of the <see cref="Auth0TokenHandler"/>
    /// </summary>
    /// <param name="cache">An instance of an <see cref="IAuth0TokenCache"/>.</param>
    /// <param name="handlerConfig">The configuration for this handler.</param>
    /// <param name="accessor"></param>
    public Auth0TokenHandler(IAuth0TokenCache cache, Auth0TokenHandlerConfig handlerConfig, HttpClientOrganizationAccessor accessor)
    {
        _cache = cache;
        _handlerConfig = handlerConfig;
        _accessor = accessor;
    }

    /// <inheritdoc cref="DelegatingHandler"/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var audience = _handlerConfig.Audience ?? _handlerConfig.AudienceResolver?.Invoke(request) ?? throw new ArgumentException("Audience cannot be computed");

        #if NET8_0_OR_GREATER
        var org = _accessor.Organization ?? _handlerConfig.Organization ?? _handlerConfig.AudienceResolver?.Invoke(request);
        var token = await _cache.GetTokenAsync(audience, org, cancellationToken);
        #else
        var token = await _cache.GetTokenAsync(audience, cancellationToken);
        #endif
        
        request.Headers.Authorization = new AuthenticationHeaderValue(Scheme, token);
        
        return await base.SendAsync(request, cancellationToken);
    }
}