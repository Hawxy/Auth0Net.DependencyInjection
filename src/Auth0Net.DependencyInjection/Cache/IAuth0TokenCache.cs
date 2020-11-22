using System;
using System.Threading.Tasks;

namespace Auth0Net.DependencyInjection.Cache
{
    /// <summary>
    /// A cache implementation 
    /// </summary>
    public interface IAuth0TokenCache
    {
        
        /// <summary>
        /// Get a JSON Web Token (JWT) Access Token for the requested audience
        /// </summary>
        /// <param name="audience">The audience you wish to request the token for.</param>
        /// <returns>The JWT</returns>
        Task<string> GetTokenAsync(string audience);
        /// <summary>
        /// Get a JSON Web Token (JWT) Access Token for the requested audience
        /// </summary>
        /// <param name="audience">The audience you wish to request the token for.</param>
        /// <returns>The JWT</returns>
        Task<string> GetTokenAsync(Uri audience);
        /// <summary>
        /// Get a JSON Web Token (JWT) Access Token for the management API.
        /// </summary>
        /// <returns>The JWT</returns>
        Task<string> GetManagementTokenAsync();
    }
}
