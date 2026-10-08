using System.Net.Http;

namespace Auth0Net.DependencyInjection.HttpClient;

/// <summary>
/// Extensions for passing On-Behalf-Of values to the <see cref="Auth0OnBehalfOfTokenHandler"/> on a per-request basis.
/// </summary>
public static class OnBehalfOfRequestExtensions
{
    private const string SubjectTokenKey = "Auth0Net.DependencyInjection.SubjectToken";

#if NET5_0_OR_GREATER
    private static readonly HttpRequestOptionsKey<string> SubjectTokenOptionsKey = new(SubjectTokenKey);
#endif

    /// <summary>
    /// Sets the user's access token that the <see cref="Auth0OnBehalfOfTokenHandler"/> exchanges for this request.
    /// </summary>
    /// <remarks>
    /// Takes precedence over <see cref="Auth0OnBehalfOfTokenHandlerConfig.SubjectTokenResolver"/>.
    /// </remarks>
    /// <param name="request">The outgoing request.</param>
    /// <param name="subjectToken">The user's access token.</param>
    /// <returns>The same <see cref="HttpRequestMessage"/>.</returns>
    public static HttpRequestMessage SetSubjectToken(this HttpRequestMessage request, string subjectToken)
    {
#if NET5_0_OR_GREATER
        request.Options.Set(SubjectTokenOptionsKey, subjectToken);
#else
        request.Properties[SubjectTokenKey] = subjectToken;
#endif
        return request;
    }

    internal static bool TryGetSubjectToken(this HttpRequestMessage request, out string? subjectToken)
    {
#if NET5_0_OR_GREATER
        if (request.Options.TryGetValue(SubjectTokenOptionsKey, out var value))
#else
        if (request.Properties.TryGetValue(SubjectTokenKey, out var stored) && stored is string value)
#endif
        {
            subjectToken = value;
            return true;
        }

        subjectToken = null;
        return false;
    }
}
