namespace Auth0Net.DependencyInjection.Cache;

/// <summary>
/// A cache implementation 
/// </summary>
public interface IAuth0TokenCache
{
        
    /// <summary>
    /// Get a JSON Web Token (JWT) Access Token for the requested audience
    /// </summary>
    /// <param name="audience">The audience you wish to request the token for.</param>
    /// <param name="token">An optional token that can cancel this request</param>
    /// <returns>The JWT</returns>
    ValueTask<string> GetTokenAsync(string audience, CancellationToken token = default);

    /// <summary>
    /// Get a JSON Web Token (JWT) Access Token for the requested audience
    /// </summary>
    /// <param name="audience">The audience you wish to request the token for.</param>
    /// <param name="token">An optional token that can cancel this request</param>
    /// <returns>The JWT</returns>
    ValueTask<string> GetTokenAsync(Uri audience, CancellationToken token = default);
    /// <summary>
    /// Get a JSON Web Token (JWT) Access Token for the management API.
    /// </summary>
    /// <param name="token">An optional token that can cancel this request</param>
    /// <returns>The JWT</returns>
    ValueTask<string> GetManagementTokenAsync(CancellationToken token = default);
}