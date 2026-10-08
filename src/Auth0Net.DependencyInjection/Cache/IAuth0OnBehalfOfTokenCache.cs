namespace Auth0Net.DependencyInjection.Cache;

/// <summary>
/// Exchanges a user's access token for an access token to another API using Auth0 On-Behalf-Of token exchange, and caches the result.
/// </summary>
public interface IAuth0OnBehalfOfTokenCache
{
    /// <summary>
    /// Get an access token for <paramref name="audience"/> on behalf of the user who holds <paramref name="subjectToken"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Results are cached per subject token, audience, scope set and organization. The cache key is a SHA-256 hash of these values, so it does not contain the subject token.
    /// A cached token is kept until 99% of its lifetime has passed, or until the subject token expires, whichever is sooner.
    /// </para>
    /// <para>
    /// The subject token's <c>exp</c> claim is read without validating the token. If it has already passed, Auth0 is not called and an
    /// <see cref="Auth0OnBehalfOfException"/> with status code 401 is thrown. If the subject token is not a readable JWT, or has no <c>exp</c> claim,
    /// the cache duration is not capped and Auth0 decides whether the token is valid.
    /// </para>
    /// </remarks>
    /// <param name="subjectToken">The user's access token, as received by this API.</param>
    /// <param name="audience">The audience of the API the exchanged token is for.</param>
    /// <param name="scope">Optional space-delimited scopes to request.</param>
    /// <param name="organization">Optional Auth0 org_id or org_name to request.</param>
    /// <param name="token">An optional token that can cancel this request.</param>
    /// <returns>The exchanged access token, its expiry and the scopes Auth0 granted.</returns>
    /// <exception cref="ArgumentException"><paramref name="subjectToken"/> or <paramref name="audience"/> is null or empty.</exception>
    /// <exception cref="Auth0OnBehalfOfException">The subject token has expired, or Auth0 rejected the exchange.</exception>
    ValueTask<OnBehalfOfToken> GetTokenAsync(string subjectToken, string audience, string? scope = null, string? organization = null, CancellationToken token = default);
}
