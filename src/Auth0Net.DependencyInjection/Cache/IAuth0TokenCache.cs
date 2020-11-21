using System;
using System.Threading.Tasks;

namespace Auth0Net.DependencyInjection.Cache
{
    /// <summary>
    /// A cache implementation 
    /// </summary>
    public interface IAuth0TokenCache
    {
        
        Task<string> GetTokenAsync(string audience);
        Task<string> GetTokenAsync(Uri audience);
        Task<string> GetManagementTokenAsync();
    }
}
