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
        var resFirst = await cache.GetTokenAsync(key);
        Assert.Equal(accessTokenFirst, resFirst);
        await Task.Delay(1000);


        var accessTokenSecond = Guid.NewGuid().ToString();

        A.CallTo(() => authClient.GetTokenAsync(A<ClientCredentialsTokenRequest>.Ignored, A<CancellationToken>.Ignored)).Returns(
            new AccessTokenResponse
            {
                AccessToken = accessTokenSecond,
                ExpiresIn = 1
            });
            
        var resSecond = await cache.GetTokenAsync(key);
        Assert.Equal(accessTokenSecond, resSecond);

        A.CallTo(() => authClient.GetTokenAsync(A<ClientCredentialsTokenRequest>.Ignored, A<CancellationToken>.Ignored))
            .MustHaveHappenedTwiceExactly();



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
}