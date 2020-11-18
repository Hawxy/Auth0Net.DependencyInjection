using System;
using System.Threading.Tasks;

namespace Auth0NET.DependencyInjection.Cache
{
    public interface IAuth0TokenCache
    {
        Task<string> GetTokenAsync(string audience);

        Task<string> GetTokenAsync(Uri audience);
        Task<string> GetManagementTokenAsync();
    }
}
