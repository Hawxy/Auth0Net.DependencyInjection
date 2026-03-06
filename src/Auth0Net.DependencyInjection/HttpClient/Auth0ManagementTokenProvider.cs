using Auth0.ManagementApi;
using Auth0Net.DependencyInjection.Cache;

namespace Auth0Net.DependencyInjection.HttpClient;
internal sealed class Auth0ManagementTokenProvider : ITokenProvider
{
    private readonly IAuth0TokenCache _cache;

    public Auth0ManagementTokenProvider(IAuth0TokenCache cache)
    {
        _cache = cache;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        return await _cache.GetManagementTokenAsync(cancellationToken);
    }
}