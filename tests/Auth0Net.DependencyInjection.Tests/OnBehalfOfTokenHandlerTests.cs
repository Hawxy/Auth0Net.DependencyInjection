using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Auth0Net.DependencyInjection.Cache;
using Auth0Net.DependencyInjection.HttpClient;
using Auth0Net.DependencyInjection.Organizations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Auth0Net.DependencyInjection.Tests;

public class OnBehalfOfTokenHandlerTests
{
    private const string Audience = "https://downstream.example.com/";
    private const string RequestUri = "https://downstream.example.com/things";

    private readonly FakeOnBehalfOfTokenCache _cache = new();
    private readonly StubHandler _inner = new();
    private readonly HttpClientOrganizationAccessor _accessor = new();

    private HttpMessageInvoker CreateInvoker(Auth0OnBehalfOfTokenHandlerConfig config, IServiceProvider services = null) =>
        new(new Auth0OnBehalfOfTokenHandler(_cache, config, _accessor, services ?? new ServiceCollection().BuildServiceProvider())
        {
            InnerHandler = _inner
        });

    [Fact]
    public async Task SetsAuthorizationHeader_FromExchangedToken()
    {
        using var invoker = CreateInvoker(new Auth0OnBehalfOfTokenHandlerConfig
        {
            Audience = Audience,
            SubjectTokenResolver = (_, _) => "subject"
        });

        using var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, RequestUri), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authorization = _inner.LastRequest!.Headers.Authorization;
        Assert.NotNull(authorization);
        Assert.Equal("Bearer", authorization.Scheme);
        Assert.Equal("exchanged:subject", authorization.Parameter);
    }

    [Fact]
    public async Task PassesAudienceAndScope_ToCache()
    {
        using var invoker = CreateInvoker(new Auth0OnBehalfOfTokenHandlerConfig
        {
            Audience = Audience,
            Scope = "read:things write:things",
            SubjectTokenResolver = (_, _) => "subject"
        });

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, RequestUri), TestContext.Current.CancellationToken);

        var call = Assert.Single(_cache.Calls);
        Assert.Equal("subject", call.SubjectToken);
        Assert.Equal(Audience, call.Audience);
        Assert.Equal("read:things write:things", call.Scope);
        Assert.Null(call.Organization);
    }

    [Fact]
    public async Task AudienceResolver_IsUsed_WhenAudienceIsNotSet()
    {
        using var invoker = CreateInvoker(new Auth0OnBehalfOfTokenHandlerConfig
        {
            AudienceResolver = r => r.RequestUri!.GetLeftPart(UriPartial.Authority),
            SubjectTokenResolver = (_, _) => "subject"
        });

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, RequestUri), TestContext.Current.CancellationToken);

        Assert.Equal("https://downstream.example.com", Assert.Single(_cache.Calls).Audience);
    }

    [Fact]
    public async Task RequestSubjectToken_TakesPrecedenceOverResolver()
    {
        var resolverCalled = false;
        using var invoker = CreateInvoker(new Auth0OnBehalfOfTokenHandlerConfig
        {
            Audience = Audience,
            SubjectTokenResolver = (_, _) =>
            {
                resolverCalled = true;
                return "from-resolver";
            }
        });

        var request = new HttpRequestMessage(HttpMethod.Get, RequestUri).SetSubjectToken("from-request");
        await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("from-request", Assert.Single(_cache.Calls).SubjectToken);
        Assert.False(resolverCalled);
    }

    [Fact]
    public async Task SubjectTokenResolver_ReceivesServiceProviderAndRequest()
    {
        var services = new ServiceCollection().AddSingleton(new SubjectTokenSource("from-services")).BuildServiceProvider();
        HttpRequestMessage resolvedFor = null;

        using var invoker = CreateInvoker(new Auth0OnBehalfOfTokenHandlerConfig
        {
            Audience = Audience,
            SubjectTokenResolver = (sp, r) =>
            {
                resolvedFor = r;
                return sp.GetRequiredService<SubjectTokenSource>().Token;
            }
        }, services);

        var request = new HttpRequestMessage(HttpMethod.Get, RequestUri);
        await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("from-services", Assert.Single(_cache.Calls).SubjectToken);
        Assert.Same(request, resolvedFor);
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(true, "")]
    [InlineData(true, "  ")]
    public async Task MissingSubjectToken_Throws401_WithoutCallingCache(bool withResolver, string resolvedToken)
    {
        using var invoker = CreateInvoker(new Auth0OnBehalfOfTokenHandlerConfig
        {
            Audience = Audience,
            SubjectTokenResolver = withResolver ? (_, _) => resolvedToken : null
        });

        var ex = await Assert.ThrowsAsync<Auth0OnBehalfOfException>(async () =>
            await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, RequestUri), TestContext.Current.CancellationToken));

        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("invalid_request", ex.Error);
        Assert.Empty(_cache.Calls);
        Assert.Null(_inner.LastRequest);
    }

    [Fact]
    public async Task EmptyRequestSubjectToken_Throws401_WithoutFallingBackToResolver()
    {
        using var invoker = CreateInvoker(new Auth0OnBehalfOfTokenHandlerConfig
        {
            Audience = Audience,
            SubjectTokenResolver = (_, _) => "from-resolver"
        });

        var request = new HttpRequestMessage(HttpMethod.Get, RequestUri).SetSubjectToken("");

        var ex = await Assert.ThrowsAsync<Auth0OnBehalfOfException>(async () =>
            await invoker.SendAsync(request, TestContext.Current.CancellationToken));

        Assert.Equal(401, ex.StatusCode);
        Assert.Empty(_cache.Calls);
    }

    [Fact]
    public async Task CacheException_Propagates()
    {
        _cache.Exception = new Auth0OnBehalfOfException(403, "access_denied", "Not allowed");
        using var invoker = CreateInvoker(new Auth0OnBehalfOfTokenHandlerConfig
        {
            Audience = Audience,
            SubjectTokenResolver = (_, _) => "subject"
        });

        var ex = await Assert.ThrowsAsync<Auth0OnBehalfOfException>(async () =>
            await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, RequestUri), TestContext.Current.CancellationToken));

        Assert.Same(_cache.Exception, ex);
        Assert.Null(_inner.LastRequest);
    }

    [Theory]
    [InlineData("accessor", "config", "resolver", "accessor")]
    [InlineData(null, "config", "resolver", "config")]
    [InlineData(null, null, "resolver", "resolver")]
    [InlineData(null, null, null, null)]
    public async Task Organization_PrefersAccessor_ThenConfig_ThenResolver(string accessorOrg, string configOrg, string resolverOrg, string expected)
    {
        _accessor.Organization = accessorOrg;
        using var invoker = CreateInvoker(new Auth0OnBehalfOfTokenHandlerConfig
        {
            Audience = Audience,
            Organization = configOrg,
            OrganizationResolver = resolverOrg is null ? null : _ => resolverOrg,
            SubjectTokenResolver = (_, _) => "subject"
        });

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, RequestUri), TestContext.Current.CancellationToken);

        Assert.Equal(expected, Assert.Single(_cache.Calls).Organization);
    }

    [Fact]
    public async Task AddOnBehalfOfToken_ResolvesHandler_FromServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAuth0OnBehalfOfTokenCache>(_cache);
        services.AddSingleton<HttpClientOrganizationAccessor>();
        services.AddSingleton(new SubjectTokenSource("from-services"));
        services.AddHttpClient<DummyClass>()
            .AddOnBehalfOfToken(x =>
            {
                x.Audience = Audience;
                x.Scope = "read:things";
                x.SubjectTokenResolver = (sp, _) => sp.GetRequiredService<SubjectTokenSource>().Token;
            })
            .ConfigurePrimaryHttpMessageHandler(() => _inner);

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(DummyClass));

        using var response = await client.GetAsync(RequestUri, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("exchanged:from-services", _inner.LastRequest!.Headers.Authorization!.Parameter);
        Assert.Equal("read:things", Assert.Single(_cache.Calls).Scope);
    }

    private sealed record SubjectTokenSource(string Token);

    private sealed record CacheCall(string SubjectToken, string Audience, string Scope, string Organization);

    private sealed class FakeOnBehalfOfTokenCache : IAuth0OnBehalfOfTokenCache
    {
        public List<CacheCall> Calls { get; } = new();

        public Exception Exception { get; set; }

        public ValueTask<OnBehalfOfToken> GetTokenAsync(string subjectToken, string audience, string scope = null, string organization = null,
            CancellationToken token = default)
        {
            Calls.Add(new CacheCall(subjectToken, audience, scope, organization));

            if (Exception is not null)
                throw Exception;

            return new ValueTask<OnBehalfOfToken>(new OnBehalfOfToken($"exchanged:{subjectToken}", DateTimeOffset.UtcNow.AddMinutes(10), scope));
        }
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
