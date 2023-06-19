using Auth0.AuthenticationApi;
using Auth0Net.DependencyInjection.Cache;
using Auth0Net.DependencyInjection.HttpClient;
using Microsoft.Extensions.Options;

namespace Auth0Net.DependencyInjection.Injectables;

internal sealed class InjectableAuthenticationApiClient : AuthenticationApiClient
{
    public InjectableAuthenticationApiClient(IOptionsSnapshot<Auth0Configuration> config, IAuthenticationConnection connection) 
        : base(UriHelpers.GetValidUri(config.Value.Domain), connection)
    {
    }
}