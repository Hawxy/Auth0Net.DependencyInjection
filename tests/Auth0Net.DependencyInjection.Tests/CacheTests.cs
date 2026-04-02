using System;
using System.Threading;
using System.Threading.Tasks;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0Net.DependencyInjection.Cache;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace Auth0Net.DependencyInjection.Tests;

public class CacheTests
{
    [Fact]
    public async Task Cache_WorksAsExpected()
    {
        var config = A.Fake<IOptionsSnapshot<Auth0Configuration>>();
        A.CallTo(() => config.Value).Returns(new Auth0Configuration
        {
            ClientId = Guid.NewGuid().ToString(),
            ClientSecret = Guid.NewGuid().ToString(),
            Domain = "https://hawxy.au.auth0.com/",
        });

        var authClient = A.Fake<IAuthenticationApiClient>();

        var accessTokenFirst = Guid.NewGuid().ToString();

        A.CallTo(() => authClient.GetTokenAsync(A<ClientCredentialsTokenRequest>.Ignored, A<CancellationToken>.Ignored)).Returns(
            new AccessTokenResponse
            {
                AccessToken = accessTokenFirst,
                ExpiresIn = 1
            });
        
        
        var cache = new Auth0TokenCache(authClient, new FusionCacheTestProvider(), new NullLogger<Auth0TokenCache>(), config);

        var key = "api://my-audience";
        var resFirst = await cache.GetTokenAsync(key, TestContext.Current.CancellationToken);
        Assert.Equal(accessTokenFirst, resFirst);
        await Task.Delay(1000, TestContext.Current.CancellationToken);


        var accessTokenSecond = Guid.NewGuid().ToString();

        A.CallTo(() => authClient.GetTokenAsync(A<ClientCredentialsTokenRequest>.Ignored, A<CancellationToken>.Ignored)).Returns(
            new AccessTokenResponse
            {
                AccessToken = accessTokenSecond,
                ExpiresIn = 1
            });
            
        var resSecond = await cache.GetTokenAsync(key, TestContext.Current.CancellationToken);
        Assert.Equal(accessTokenSecond, resSecond);

        A.CallTo(() => authClient.GetTokenAsync(A<ClientCredentialsTokenRequest>.Ignored, A<CancellationToken>.Ignored))
            .MustHaveHappenedTwiceExactly();
    }

    
    [Fact]
    public async Task Cache_WhenGivenOrgId_PassesOrgIdToTokenRequest()
    {
        const string orgId = "org_123456";

        var config = A.Fake<IOptionsSnapshot<Auth0Configuration>>();
        A.CallTo(() => config.Value).Returns(new Auth0Configuration
        {
            ClientId = Guid.NewGuid().ToString(),
            ClientSecret = Guid.NewGuid().ToString(),
            Domain = "https://hawxy.au.auth0.com/",
        });

        var authClient = A.Fake<IAuthenticationApiClient>();
        A.CallTo(() => authClient.GetTokenAsync(A<ClientCredentialsTokenRequest>.Ignored, A<CancellationToken>.Ignored))
            .Returns(new AccessTokenResponse { AccessToken = "token", ExpiresIn = 60 });

        var cache = new Auth0TokenCache(authClient, new FusionCacheTestProvider(), new NullLogger<Auth0TokenCache>(), config);

        await cache.GetTokenAsync("api://my-audience", orgId, TestContext.Current.CancellationToken);

        A.CallTo(() => authClient.GetTokenAsync(
                A<ClientCredentialsTokenRequest>.That.Matches(r => r.Organization == orgId),
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Cache_UsesFusionCacheInstance_WhenConfigured()
    {
        const string customCacheName = "my-custom-cache";

        var config = A.Fake<IOptionsSnapshot<Auth0Configuration>>();
        A.CallTo(() => config.Value).Returns(new Auth0Configuration
        {
            ClientId = Guid.NewGuid().ToString(),
            ClientSecret = Guid.NewGuid().ToString(),
            Domain = "https://hawxy.au.auth0.com/",
            FusionCacheInstance = customCacheName
        });

        var authClient = A.Fake<IAuthenticationApiClient>();
        A.CallTo(() => authClient.GetTokenAsync(A<ClientCredentialsTokenRequest>.Ignored, A<CancellationToken>.Ignored))
            .Returns(new AccessTokenResponse { AccessToken = "token", ExpiresIn = 60 });

        var provider = new CapturingFusionCacheProvider();
        _ = new Auth0TokenCache(authClient, provider, new NullLogger<Auth0TokenCache>(), config);

        Assert.Equal(customCacheName, provider.LastRequestedCacheName);
    }

    [Fact]
    public async Task Cache_UsesDefaultFusionCacheInstance_WhenNotConfigured()
    {
        var config = A.Fake<IOptionsSnapshot<Auth0Configuration>>();
        A.CallTo(() => config.Value).Returns(new Auth0Configuration
        {
            ClientId = Guid.NewGuid().ToString(),
            ClientSecret = Guid.NewGuid().ToString(),
            Domain = "https://hawxy.au.auth0.com/",
        });

        var authClient = A.Fake<IAuthenticationApiClient>();

        var provider = new CapturingFusionCacheProvider();
        _ = new Auth0TokenCache(authClient, provider, new NullLogger<Auth0TokenCache>(), config);

        Assert.Equal(Constants.FusionCacheInstance, provider.LastRequestedCacheName);
    }

    private sealed class FusionCacheTestProvider : IFusionCacheProvider
    {
        public IFusionCache GetCache(string cacheName)
        {
            return new FusionCache(new FusionCacheOptions());
        }

        public IFusionCache GetCacheOrNull(string cacheName)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class CapturingFusionCacheProvider : IFusionCacheProvider
    {
        public string LastRequestedCacheName { get; private set; }

        public IFusionCache GetCache(string cacheName)
        {
            LastRequestedCacheName = cacheName;
            return new FusionCache(new FusionCacheOptions());
        }

        public IFusionCache GetCacheOrNull(string cacheName)
        {
            LastRequestedCacheName = cacheName;
            return new FusionCache(new FusionCacheOptions());
        }
    }
}