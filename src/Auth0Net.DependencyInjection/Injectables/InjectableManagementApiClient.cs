using Auth0.ManagementApi;
using Auth0Net.DependencyInjection.Cache;
using Auth0Net.DependencyInjection.HttpClient;
using Microsoft.Extensions.Options;

namespace Auth0Net.DependencyInjection.Injectables
{
    internal class InjectableManagementApiClient : ManagementApiClient
    {
        public InjectableManagementApiClient(IOptionsSnapshot<Auth0Configuration> config, IManagementConnection managementConnection)
            : base(null, UriHelpers.GetValidUri(config.Value.Domain), managementConnection)
        {
        }
    }
}
