using System.Net.Http;

namespace Auth0Net.DependencyInjection.HttpClient;

/// <summary>
/// Configuration used by the underlying <see cref="Auth0OnBehalfOfTokenHandler"/>.
/// </summary>
public sealed class Auth0OnBehalfOfTokenHandlerConfig
{
    /// <summary>
    /// The resource identifier - aka Audience - of the API the exchanged token is for.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// A resolver that will compute the audience during the request.
    /// </summary>
    /// <remarks>
    /// A value set in <see cref="Audience"/> will take precedence over any resolver set here - be careful not to mix the two.
    /// </remarks>
    public Func<HttpRequestMessage, string>? AudienceResolver { get; set; }

    /// <summary>
    /// Optional space-delimited scopes to request.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// The org_id or org_name to request the token for.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// A resolver that will compute the org_id or org_name during the request.
    /// </summary>
    /// <remarks>
    /// A value set in <see cref="Organization"/> will take precedence over any resolver set here.
    /// </remarks>
    public Func<HttpRequestMessage, string?>? OrganizationResolver { get; set; }

    /// <summary>
    /// A resolver that returns the user's access token to exchange, such as the bearer token of the current incoming request.
    /// </summary>
    /// <remarks>
    /// A token set on the request with <see cref="OnBehalfOfRequestExtensions.SetSubjectToken"/> takes precedence over this resolver.
    /// The <see cref="IServiceProvider"/> is the one the HttpClientFactory creates the handler from, not the scope of the incoming request.
    /// </remarks>
    public Func<IServiceProvider, HttpRequestMessage, string?>? SubjectTokenResolver { get; set; }
}
