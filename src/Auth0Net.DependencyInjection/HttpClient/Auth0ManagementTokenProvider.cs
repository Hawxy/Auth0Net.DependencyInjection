using Auth0.ManagementApi;
using Auth0Net.DependencyInjection.Cache;

namespace Auth0Net.DependencyInjection.HttpClient;
internal sealed class Auth0ManagementTokenProvider : ITokenProvider
{
    private readonly IAuth0TokenCache _cache;
    private readonly string _audience;
    
    public Auth0ManagementTokenProvider(IAuth0TokenCache cache, string audience)
    {
        _cache = cache;
        _audience = audience;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        return await _cache.GetTokenAsync(_audience, cancellationToken);
    }
}