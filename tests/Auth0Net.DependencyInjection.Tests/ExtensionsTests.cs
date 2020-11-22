using System.Linq;
using Auth0.AuthenticationApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Auth0Net.DependencyInjection.Tests
{
    public class ExtensionTests
    {
        [Fact]
        public void AddAuth0AuthenticationClientCore_ThrowsOnInvalidDomain()
        {
            var services = new ServiceCollection().AddAuth0AuthenticationClientCore("").Services.BuildServiceProvider();

            Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<AuthenticationApiClient>());
        }

        [Fact]
        public void AddAuth0AuthenticationClientCore_ContainerResolvesAuthenticationClient()
        {
            var services = new ServiceCollection().AddAuth0AuthenticationClientCore("test-url.au.auth0.com").Services;

            var serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(AuthenticationApiClient));

            Assert.NotNull(serviceDescriptor);
            Assert.Equal(ServiceLifetime.Scoped, ServiceLifetime.Scoped);


            var provider = services.BuildServiceProvider();

            var authenticationClient = provider.GetService<AuthenticationApiClient>();
            Assert.NotNull(authenticationClient);

        }
    }
}
