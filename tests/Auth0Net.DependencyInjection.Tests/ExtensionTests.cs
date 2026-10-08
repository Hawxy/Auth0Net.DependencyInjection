using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using Auth0.AuthenticationApi;
using Auth0.ManagementApi;
using Auth0Net.DependencyInjection.Cache;
using Auth0Net.DependencyInjection.Injectables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace Auth0Net.DependencyInjection.Tests;

public class ExtensionTests
{
    [Fact]
    public void AddAuth0AuthenticationClientCore_Throws_OnInvalidDomain()
    {
        var services = new ServiceCollection().AddAuth0AuthenticationClient("").Services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IAuthenticationApiClient>());
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

        Assert.Throws<InvalidOperationException>(() => services.AddAuth0AuthenticationClient("test-url.au.auth0.com"));
    }

    [Fact]
    public void AddAuth0AuthenticationClientCore_Resolves_AuthenticationClient()
    {
        var domain = "test-url.au.auth0.com";

        var services = new ServiceCollection().AddAuth0AuthenticationClient(domain).Services;

        var serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IAuthenticationApiClient)
                                                             && x.ImplementationType == typeof(InjectableAuthenticationApiClient));

        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, ServiceLifetime.Scoped);

        var provider = services.BuildServiceProvider();

        var authenticationClient = provider.GetService<IAuthenticationApiClient>();
        Assert.NotNull(authenticationClient);
        Assert.IsType<InjectableAuthenticationApiClient>(authenticationClient);

        var authenticationHttpClient = provider.GetService<IAuthenticationConnection>();
        Assert.NotNull(authenticationHttpClient);

        var configuration = provider.GetService<IOptions<Auth0Configuration>>();
        Assert.NotNull(configuration);
        Assert.Equal(domain, configuration.Value.Domain);
    }

    [Fact]
    public void AddAuth0AuthenticationClient_Throws_AuthenticationClientAlreadyRegistered()
    {
        var services = new ServiceCollection().AddAuth0AuthenticationClient("").Services;

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

        Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IAuthenticationApiClient>());
    }

    [Fact]
    public void AddAuth0AuthenticationClient_Resolves_AuthenticationClient()
    {
        var domain = "test.au.auth0.com";
        var clientId = "fake-id";
        var clientSecret = "fake-secret";

        var services = new ServiceCollection().AddAuth0AuthenticationClient(x =>
        {
            x.Domain = domain;
            x.ClientId = clientId;
            x.ClientSecret = clientSecret;
        }).Services;

        var serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IAuthenticationApiClient));

        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, ServiceLifetime.Scoped);

        var provider = services.BuildServiceProvider();

        var authenticationClient = provider.GetService<IAuthenticationApiClient>();
        Assert.NotNull(authenticationClient);
        Assert.IsType<InjectableAuthenticationApiClient>(authenticationClient);

        var authenticationHttpClient = provider.GetService<IAuthenticationConnection>();
        Assert.NotNull(authenticationHttpClient);

        var tokenCache = provider.GetService<IAuth0TokenCache>();
        Assert.NotNull(tokenCache);

        var fusionCache = provider.GetService<IFusionCacheProvider>();
        Assert.NotNull(fusionCache);

        var configuration = provider.GetService<IOptions<Auth0Configuration>>();

        Assert.Equal(domain, configuration.Value.Domain);
        Assert.Equal(clientId, configuration.Value.ClientId);
        Assert.Equal(clientSecret, configuration.Value.ClientSecret);
    }

    public sealed record FakeConfiguration(string Domain, string ClientId, string ClientSecret);
    
    [Fact]
    public void AddAuth0AuthenticationClient_WithServiceCollection_CanBeResolved()
    {
        var domain = "test.au.auth0.com";
        var clientId = "fake-id";
        var clientSecret = "fake-secret";
        
        var services = new ServiceCollection();

        services.AddSingleton(new FakeConfiguration(domain, clientId, clientSecret));

        services.AddAuth0AuthenticationClient((x, p) =>
        {
            var config = p.GetRequiredService<FakeConfiguration>();
            
            x.Domain = config.Domain;
            x.ClientId = config.ClientId;
            x.ClientSecret = config.ClientSecret;
        });

        var collection = services.BuildServiceProvider();
        
        var authenticationClient = collection.GetService<IAuthenticationApiClient>();
        Assert.NotNull(authenticationClient);
        Assert.IsType<InjectableAuthenticationApiClient>(authenticationClient);
    }

    [Fact]
    public void AddAccessToken_Rejects_InvalidConfig()
    {
        Assert.Throws<ArgumentException>(() =>
            new ServiceCollection().AddHttpClient<DummyClass>(x => { }).AddAccessToken(x => { }));
    }

    [Fact]
    public void AddManagementClient_Can_BeResolved()
    {
        var customDomain = "custom-domain.com";
        var clientId = "fake-id";
        var clientSecret = "fake-secret";

        var collection = new ServiceCollection();

        collection.AddAuth0AuthenticationClient(x =>
        {
            x.Domain = customDomain;
            x.ClientId = clientId;
            x.ClientSecret = clientSecret;
        });

        var defaultDomain = "tenant.au.auth0.com";

        collection.AddAuth0ManagementClient();
        collection.AddHttpClient<DummyClass>();

        var services = collection.BuildServiceProvider();

        var client = services.GetService<IManagementApiClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddAuth0OnBehalfOf_Throws_OnResolve_WithoutAuthenticationClient()
    {
        var services = new ServiceCollection();
        services.AddAuth0OnBehalfOf(x =>
        {
            x.ClientId = "obo-id";
            x.ClientSecret = "obo-secret";
        });

        var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IAuth0OnBehalfOfTokenCache>());
        Assert.Contains(nameof(Auth0Extensions.AddAuth0AuthenticationClient), ex.Message);
    }

    [Fact]
    public void AddAuth0OnBehalfOf_Resolves_WhenRegisteredBeforeAuthenticationClient()
    {
        var services = new ServiceCollection();
        services.AddAuth0OnBehalfOf(x =>
        {
            x.ClientId = "obo-id";
            x.ClientSecret = "obo-secret";
        });
        services.AddAuth0AuthenticationClient("test.au.auth0.com");

        var provider = services.BuildServiceProvider();

        Assert.IsType<Auth0OnBehalfOfTokenCache>(provider.GetRequiredService<IAuth0OnBehalfOfTokenCache>());
    }

    [Fact]
    public void AddAuth0OnBehalfOf_Resolves_WithDomainOnlyAuthenticationClient()
    {
        var services = new ServiceCollection();
        services.AddAuth0AuthenticationClient("test.au.auth0.com");
        services.AddAuth0OnBehalfOf(x =>
        {
            x.ClientId = "obo-id";
            x.ClientSecret = "obo-secret";
        });

        var provider = services.BuildServiceProvider();

        Assert.IsType<Auth0OnBehalfOfTokenCache>(provider.GetRequiredService<IAuth0OnBehalfOfTokenCache>());
        Assert.NotNull(provider.GetRequiredService<IFusionCacheProvider>().GetCache(Constants.FusionCacheInstance));
        Assert.Null(provider.GetService<IAuth0TokenCache>());
    }

    [Fact]
    public void AddAuth0OnBehalfOf_Resolves_WithFullAuthenticationClient()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new FakeConfiguration("test.au.auth0.com", "obo-id", "obo-secret"));
        services.AddAuth0AuthenticationClient(x =>
        {
            x.Domain = "test.au.auth0.com";
            x.ClientId = "m2m-id";
            x.ClientSecret = "m2m-secret";
        });
        services.AddAuth0OnBehalfOf((x, p) =>
        {
            var config = p.GetRequiredService<FakeConfiguration>();
            x.ClientId = config.ClientId;
            x.ClientSecret = config.ClientSecret;
        });

        Assert.Single(services, x => x.ServiceType == typeof(Auth0FusionCacheMarker));

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IAuth0TokenCache>());
        Assert.NotNull(provider.GetRequiredService<IAuth0OnBehalfOfTokenCache>());
        Assert.NotNull(provider.GetRequiredService<IFusionCacheProvider>().GetCache(Constants.FusionCacheInstance));

        var options = provider.GetRequiredService<IOptions<Auth0OnBehalfOfConfiguration>>().Value;
        Assert.Equal("obo-id", options.ClientId);
        Assert.Equal("obo-secret", options.ClientSecret);
        Assert.Equal("m2m-id", provider.GetRequiredService<IOptions<Auth0Configuration>>().Value.ClientId);
    }

    [Fact]
    public void AddAuth0OnBehalfOf_Resolves_WithClientAssertion()
    {
        var services = new ServiceCollection();
        services.AddAuth0AuthenticationClient("test.au.auth0.com");
        services.AddAuth0OnBehalfOf(x =>
        {
            x.ClientId = "obo-id";
            x.ClientAssertionSecurityKey = new RsaSecurityKey(RSA.Create());
            x.ClientAssertionSecurityKeyAlgorithm = SecurityAlgorithms.RsaSha256;
        });

        Assert.NotNull(services.BuildServiceProvider().GetRequiredService<IAuth0OnBehalfOfTokenCache>());
    }

    [Theory]
    [InlineData("", "obo-secret", false)]
    [InlineData("obo-id", "", false)]
    [InlineData("obo-id", null, false)]
    [InlineData("obo-id", null, true)]
    public void AddAuth0OnBehalfOf_Rejects_EmptyCredentials(string clientId, string clientSecret, bool keyWithoutAlgorithm)
    {
        var services = new ServiceCollection();
        services.AddAuth0AuthenticationClient("test.au.auth0.com");
        services.AddAuth0OnBehalfOf(x =>
        {
            x.ClientId = clientId;
            x.ClientSecret = clientSecret;
            if (keyWithoutAlgorithm)
                x.ClientAssertionSecurityKey = new RsaSecurityKey(RSA.Create());
        });

        var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IAuth0OnBehalfOfTokenCache>());
    }

    [Fact]
    public void AddOnBehalfOfToken_Rejects_InvalidConfig()
    {
        Assert.Throws<ArgumentException>(() =>
            new ServiceCollection().AddHttpClient<DummyClass>(x => { }).AddOnBehalfOfToken(x => { }));
    }

    [Fact]
    public void AddOnBehalfOfToken_Throws_WithoutAddAuth0OnBehalfOf()
    {
        var services = new ServiceCollection();
        services.AddAuth0AuthenticationClient("test.au.auth0.com");
        services.AddHttpClient<DummyClass>().AddOnBehalfOfToken(x => x.Audience = "https://downstream.example.com/");

        var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(DummyClass)));
        Assert.Contains(nameof(Auth0Extensions.AddAuth0OnBehalfOf), ex.Message);
    }

    [Fact]
    public void AddOnBehalfOfToken_Resolves_WithAddAuth0OnBehalfOf()
    {
        var services = new ServiceCollection();
        services.AddAuth0AuthenticationClient("test.au.auth0.com");
        services.AddAuth0OnBehalfOf(x =>
        {
            x.ClientId = "obo-id";
            x.ClientSecret = "obo-secret";
        });
        services.AddHttpClient<DummyClass>().AddOnBehalfOfToken(x => x.Audience = "https://downstream.example.com/");

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(DummyClass)));
    }

}

public class DummyClass
{
}