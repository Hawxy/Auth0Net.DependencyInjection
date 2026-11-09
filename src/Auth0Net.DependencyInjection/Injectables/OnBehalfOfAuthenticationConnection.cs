using System.Net.Http;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;

namespace Auth0Net.DependencyInjection.Injectables;

/// <summary>
/// Adds the <c>requested_token_type</c> parameter required by Auth0 to On-Behalf-Of token exchange requests, as Auth0.AuthenticationApi does not send it.
/// </summary>
/// <remarks>
/// Can be removed once https://github.com/auth0/auth0.net/pull/1133 is released.
/// </remarks>
internal sealed class OnBehalfOfAuthenticationConnection : IAuthenticationConnection
{
    private const string TokenExchangeGrantType = "urn:ietf:params:oauth:grant-type:token-exchange";

    private readonly IAuthenticationConnection _inner;

    public OnBehalfOfAuthenticationConnection(IAuthenticationConnection inner)
    {
        _inner = inner;
    }

    public Task<T> GetAsync<T>(Uri uri, IDictionary<string, string> headers = null!, CancellationToken cancellationToken = default) =>
        _inner.GetAsync<T>(uri, headers, cancellationToken);

    public Task<T> SendAsync<T>(HttpMethod method, Uri uri, object body, IDictionary<string, string> headers = null!, CancellationToken cancellationToken = default)
    {
        // A value already set by Auth0.AuthenticationApi is left as is.
        if (body is IDictionary<string, string> form
            && form.TryGetValue("grant_type", out var grantType) && grantType == TokenExchangeGrantType
            && !form.ContainsKey("requested_token_type"))
        {
            form["requested_token_type"] = TokenType.AccessToken;
        }

        return _inner.SendAsync<T>(method, uri, body, headers, cancellationToken);
    }
}
