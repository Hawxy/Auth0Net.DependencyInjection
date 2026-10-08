using System.Net.Http;
using System.Net.Http.Headers;
using Auth0Net.DependencyInjection.Cache;
using Auth0Net.DependencyInjection.Organizations;

namespace Auth0Net.DependencyInjection.HttpClient;

/// <summary>
/// A <see cref="DelegatingHandler"/> that exchanges the user's access token using Auth0 On-Behalf-Of token exchange, and adds the exchanged token as the authentication header.
/// </summary>
public class Auth0OnBehalfOfTokenHandler : DelegatingHandler
{
    private const string Scheme = "Bearer";
    private readonly IAuth0OnBehalfOfTokenCache _cache;
    private readonly Auth0OnBehalfOfTokenHandlerConfig _handlerConfig;
    private readonly HttpClientOrganizationAccessor _accessor;
    private readonly IServiceProvider _services;

    /// <summary>
    /// Constructs a new instance of the <see cref="Auth0OnBehalfOfTokenHandler"/>
    /// </summary>
    /// <param name="cache">An instance of an <see cref="IAuth0OnBehalfOfTokenCache"/>.</param>
    /// <param name="handlerConfig">The configuration for this handler.</param>
    /// <param name="accessor">The accessor for the organization set by an organization scope.</param>
    /// <param name="services">The <see cref="IServiceProvider"/> passed to <see cref="Auth0OnBehalfOfTokenHandlerConfig.SubjectTokenResolver"/>.</param>
    public Auth0OnBehalfOfTokenHandler(IAuth0OnBehalfOfTokenCache cache, Auth0OnBehalfOfTokenHandlerConfig handlerConfig, HttpClientOrganizationAccessor accessor, IServiceProvider services)
    {
        _cache = cache;
        _handlerConfig = handlerConfig;
        _accessor = accessor;
        _services = services;
    }

    /// <inheritdoc cref="DelegatingHandler"/>
    /// <exception cref="Auth0OnBehalfOfException">No subject token is available, the subject token has expired, or Auth0 rejected the exchange.</exception>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var audience = _handlerConfig.Audience ?? _handlerConfig.AudienceResolver?.Invoke(request) ?? throw new ArgumentException("Audience cannot be computed");

        var org = _accessor.Organization ?? _handlerConfig.Organization ?? _handlerConfig.OrganizationResolver?.Invoke(request);

        var subjectToken = request.TryGetSubjectToken(out var requestToken)
            ? requestToken
            : _handlerConfig.SubjectTokenResolver?.Invoke(_services, request);

        if (string.IsNullOrWhiteSpace(subjectToken))
            throw Auth0OnBehalfOfException.SubjectTokenMissing();

        var token = await _cache.GetTokenAsync(subjectToken!, audience, _handlerConfig.Scope, org, cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue(Scheme, token.AccessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
