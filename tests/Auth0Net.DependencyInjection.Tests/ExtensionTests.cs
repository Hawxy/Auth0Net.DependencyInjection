using System;
using System.Linq;
using Auth0.AuthenticationApi;
using Auth0Net.DependencyInjection.Cache;
using LazyCache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Auth0Net.DependencyInjection.Tests
{
    public class ExtensionTests
    {
        [Fact]
        public void AddAuth0AuthenticationClientCore_Throws_OnInvalidDomain()
        {
            var services = new ServiceCollection().AddAuth0AuthenticationClientCore("").Services.BuildServiceProvider();

            Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<AuthenticationApiClient>());
        }

        [Fact]
        public void AddAuth0AuthenticationClientCore_Throws_AuthenticationClientAlreadyRegistered()
        {
            var services = new ServiceCollection().AddAuth0AuthenticationClient(x =>
            {
                x.Domain = "";
                x.ClientId = "";
                x.ClientSecret = "";
            }).Services;

            Assert.Throws<InvalidOperationException>(() => services.AddAuth0AuthenticationClientCore("test-url.au.auth0.com"));
        }

        [Fact]
        public void AddAuth0AuthenticationClientCore_Resolves_AuthenticationClient()
        {
            var domain = "test-url.au.auth0.com";

            var services = new ServiceCollection().AddAuth0AuthenticationClientCore(domain).Services;

            var serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(AuthenticationApiClient));

            Assert.NotNull(serviceDescriptor);
            Assert.Equal(ServiceLifetime.Scoped, ServiceLifetime.Scoped);
            
            var provider = services.BuildServiceProvider();

            var authenticationClient = provider.GetService<AuthenticationApiClient>();
            Assert.NotNull(authenticationClient);

            var authenticationHttpClient = provider.GetService<IAuthenticationConnection>();
            Assert.NotNull(authenticationHttpClient);

            var configuration = provider.GetService<IOptions<Auth0Configuration>>();
            Assert.NotNull(configuration);
            Assert.Equal(domain, configuration.Value.Domain);
        }

        [Fact]
        public void AddAuth0AuthenticationClient_Throws_AuthenticationClientAlreadyRegistered()
        {
            var services = new ServiceCollection().AddAuth0AuthenticationClientCore("").Services;

            Assert.Throws<InvalidOperationException>(() => services.AddAuth0AuthenticationClient(x =>
            {
                x.Domain = "";
                x.ClientId = "";
                x.ClientSecret = "";
            }));
        }

        [Fact]
        public void AddAuth0AuthenticationClient_Throws_InvalidConfiguration()
        {
            var services = new ServiceCollection().AddAuth0AuthenticationClient(x =>
            {
                x.Domain = "";
                x.ClientId = "";
                x.ClientSecret = "";
            }).Services.BuildServiceProvider();

            Assert.Throws<OptionsValidationException>(() =>  services.GetRequiredService<AuthenticationApiClient>());
        }

        [Fact]
        public void AddAuth0AuthenticationClient_Resolves_AuthenticationClient()
        {
            var domain = "test.au.auth0.com";
            var clientId = "fake-id";
            var clientSecret = "fake-secret";
            var renewal = TimeSpan.FromMinutes(60);

            var services = new ServiceCollection().AddAuth0AuthenticationClient(x =>
            {
                x.Domain = domain;
                x.ClientId = clientId;
                x.ClientSecret = clientSecret;
                x.RenewTokenAt = renewal;
            }).Services;

            var serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(AuthenticationApiClient));

            Assert.NotNull(serviceDescriptor);
            Assert.Equal(ServiceLifetime.Scoped, ServiceLifetime.Scoped);

            var provider = services.BuildServiceProvider();

            var authenticationClient = provider.GetService<AuthenticationApiClient>();
            Assert.NotNull(authenticationClient);

            var authenticationHttpClient = provider.GetService<IAuthenticationConnection>();
            Assert.NotNull(authenticationHttpClient);

            var tokenCache = provider.GetService<IAuth0TokenCache>();
            Assert.NotNull(tokenCache);

            var appCache = provider.GetService<IAppCache>();
            Assert.NotNull(appCache);

            var configuration = provider.GetService<IOptions<Auth0Configuration>>();

            Assert.Equal(domain, configuration.Value.Domain);
            Assert.Equal(clientId, configuration.Value.ClientId);
            Assert.Equal(clientSecret, configuration.Value.ClientSecret);
            Assert.Equal(renewal, configuration.Value.RenewTokenAt);
        }

        [Fact]
        public void AddAccessToken_Rejects_InvalidConfig()
        {
            Assert.Throws<ArgumentException>(() =>
                new ServiceCollection().AddHttpClient<DummyClass>(x => { }).AddAccessToken(x => { }));
        }

    }

    public class DummyClass
    {
    }

}
