using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Auth0Net.DependencyInjection.Cache;
using Auth0Net.DependencyInjection.HttpClient;
using Auth0Net.DependencyInjection.Organizations;
using FakeItEasy;
using Xunit;

namespace Auth0Net.DependencyInjection.Tests;

public class OrganizationAccessorTests
{
    [Fact]
    public async Task OrganizationScope_AppliesOrganizationToTokenHandler()
    {
        var cache = A.Fake<IAuth0TokenCache>();
        A.CallTo(() => cache.GetTokenAsync(A<string>._, A<string?>._, A<CancellationToken>._))
            .Returns("access-token");

        var accessor = new HttpClientOrganizationAccessor { Organization = "org-from-accessor" };
        var config = new Auth0TokenHandlerConfig { Audience = "api://test" };

        using var invoker = BuildInvoker(cache, config, accessor);
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com"), CancellationToken.None);

        A.CallTo(() => cache.GetTokenAsync("api://test", "org-from-accessor", A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task TokenHandler_UsesConfigOrganization_WhenAccessorIsEmpty()
    {
        var cache = A.Fake<IAuth0TokenCache>();
        A.CallTo(() => cache.GetTokenAsync(A<string>._, A<string?>._, A<CancellationToken>._))
            .Returns("access-token");

        var accessor = new HttpClientOrganizationAccessor();
        var config = new Auth0TokenHandlerConfig { Audience = "api://test", Organization = "org-from-config" };

        using var invoker = BuildInvoker(cache, config, accessor);
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com"), CancellationToken.None);

        A.CallTo(() => cache.GetTokenAsync("api://test", "org-from-config", A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task TokenHandler_AccessorOrganizationTakesPrecedenceOverConfig()
    {
        var cache = A.Fake<IAuth0TokenCache>();
        A.CallTo(() => cache.GetTokenAsync(A<string>._, A<string?>._, A<CancellationToken>._))
            .Returns("access-token");

        var accessor = new HttpClientOrganizationAccessor { Organization = "org-from-accessor" };
        var config = new Auth0TokenHandlerConfig { Audience = "api://test", Organization = "org-from-config" };

        using var invoker = BuildInvoker(cache, config, accessor);
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com"), CancellationToken.None);

        A.CallTo(() => cache.GetTokenAsync("api://test", "org-from-accessor", A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    private static HttpMessageInvoker BuildInvoker(
        IAuth0TokenCache cache,
        Auth0TokenHandlerConfig config,
        HttpClientOrganizationAccessor accessor)
    {
        var handler = new Auth0TokenHandler(cache, config, accessor)
        {
            InnerHandler = new StubInnerHandler()
        };
        return new HttpMessageInvoker(handler);
    }

    private sealed class StubInnerHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
