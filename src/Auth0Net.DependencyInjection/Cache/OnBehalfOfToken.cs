namespace Auth0Net.DependencyInjection.Cache;

/// <summary>
/// An access token issued by an On-Behalf-Of token exchange.
/// </summary>
/// <param name="AccessToken">The access token.</param>
/// <param name="ExpiresAt">When the access token expires.</param>
/// <param name="Scope">The scopes Auth0 granted, which may be narrower than the scopes requested.</param>
public sealed record OnBehalfOfToken(string AccessToken, DateTimeOffset ExpiresAt, string? Scope)
{
    /// <summary>
    /// Returns a description of the token that omits <see cref="AccessToken"/>.
    /// </summary>
    public override string ToString() => $"{nameof(OnBehalfOfToken)} {{ {nameof(ExpiresAt)} = {ExpiresAt:O}, {nameof(Scope)} = {Scope} }}";
}
