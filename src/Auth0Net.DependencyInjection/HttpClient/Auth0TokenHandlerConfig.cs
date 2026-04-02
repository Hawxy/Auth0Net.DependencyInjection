using System.Net.Http;

namespace Auth0Net.DependencyInjection.HttpClient;

/// <summary>
/// Configuration used by the underlying <see cref="Auth0TokenHandler"/>. 
/// </summary>
public sealed class Auth0TokenHandlerConfig
{
    /// <summary>
    /// The resource identifier - aka Audience - you wish to request the token for.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// A resolver that will compute the audience during the request.
    /// This is useful if your services follow a pattern of audience naming, or if you're using other handler integrations such as service discovery.
    /// </summary>
    /// <remarks>
    /// A value set in <see cref="Audience"/> will take precedence over any resolver set here - be careful not to mix the two.
    /// </remarks>
    public Func<HttpRequestMessage, string>? AudienceResolver { get; set; }
    
}