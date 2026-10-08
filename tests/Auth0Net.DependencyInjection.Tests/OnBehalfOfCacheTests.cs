using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0.Core.Exceptions;
using Auth0Net.DependencyInjection.Cache;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace Auth0Net.DependencyInjection.Tests;

public sealed class OnBehalfOfCacheTests : IDisposable
{
    private const string Audience = "https://auditing.example.com/";
    private const string ClientId = "obo-client-id";
    private const string ClientSecret = "obo-client-secret";

    private readonly IAuthenticationApiClient _authClient = A.Fake<IAuthenticationApiClient>();
    private readonly FusionCache _fusionCache = new(new FusionCacheOptions { EnableSyncEventHandlersExecution = true });
    private readonly Auth0OnBehalfOfTokenCache _cache;

    public OnBehalfOfCacheTests()
    {
        A.CallTo(() => _authClient.GetTokenOnBehalfOfAsync(A<OnBehalfOfTokenRequest>.Ignored, A<CancellationToken>.Ignored))
            .ReturnsLazily((OnBehalfOfTokenRequest r, CancellationToken _) => Task.FromResult(new OnBehalfOfTokenResponse
            {
                AccessToken = Guid.NewGuid().ToString(),
                ExpiresIn = 600,
                Scope = r.Scope
            }));

        _cache = new Auth0OnBehalfOfTokenCache(_authClient, new SharedFusionCacheProvider(_fusionCache),
            new NullLogger<Auth0OnBehalfOfTokenCache>(),
            Options.Create(new Auth0Configuration { Domain = "https://hawxy.au.auth0.com/" }),
            Options.Create(new Auth0OnBehalfOfConfiguration { ClientId = ClientId, ClientSecret = ClientSecret }));
    }

    public void Dispose() => _fusionCache.Dispose();

    private void AssertExchangeCount(int count) =>
        A.CallTo(() => _authClient.GetTokenOnBehalfOfAsync(A<OnBehalfOfTokenRequest>.Ignored, A<CancellationToken>.Ignored))
            .MustHaveHappened(count, Times.Exactly);

    [Fact]
    public async Task SameSubjectAudienceAndScope_HitsCache_RegardlessOfScopeOrder()
    {
        var subject = CreateJwt(DateTimeOffset.UtcNow.AddHours(1));
        var ct = TestContext.Current.CancellationToken;

        var first = await _cache.GetTokenAsync(subject, Audience, "audit:read audit:export", "org_1", ct);
        var second = await _cache.GetTokenAsync(subject, Audience, "audit:export audit:read", "org_1", ct);
        var third = await _cache.GetTokenAsync(subject, Audience, "  audit:read\taudit:export audit:read ", "org_1", ct);

        Assert.Equal(first, second);
        Assert.Equal(first, third);
        AssertExchangeCount(1);
    }

    [Fact]
    public async Task DifferentSubjectScopeOrOrganization_MissesCache()
    {
        var subject = CreateJwt(DateTimeOffset.UtcNow.AddHours(1));
        var otherSubject = CreateJwt(DateTimeOffset.UtcNow.AddHours(1), "auth0|other");
        var ct = TestContext.Current.CancellationToken;

        await _cache.GetTokenAsync(subject, Audience, "audit:read", "org_1", ct);
        await _cache.GetTokenAsync(otherSubject, Audience, "audit:read", "org_1", ct);
        await _cache.GetTokenAsync(subject, Audience, "audit:read audit:export", "org_1", ct);
        await _cache.GetTokenAsync(subject, Audience, "audit:read", "org_2", ct);
        await _cache.GetTokenAsync(subject, Audience, "audit:read", null, ct);

        AssertExchangeCount(5);
    }

    [Fact]
    public async Task ReturnsGrantedScopeAndExpiry()
    {
        A.CallTo(() => _authClient.GetTokenOnBehalfOfAsync(A<OnBehalfOfTokenRequest>.Ignored, A<CancellationToken>.Ignored))
            .Returns(new OnBehalfOfTokenResponse { AccessToken = "exchanged", ExpiresIn = 600, Scope = "audit:read" });

        var before = DateTimeOffset.UtcNow;
        var result = await _cache.GetTokenAsync(CreateJwt(DateTimeOffset.UtcNow.AddHours(1)), Audience, "audit:read audit:export",
            token: TestContext.Current.CancellationToken);

        Assert.Equal("exchanged", result.AccessToken);
        Assert.Equal("audit:read", result.Scope);
        Assert.InRange(result.ExpiresAt, before.AddSeconds(600), DateTimeOffset.UtcNow.AddSeconds(600));
        Assert.DoesNotContain("exchanged", result.ToString());
    }

    [Fact]
    public async Task CacheDuration_IsCappedBySubjectTokenExpiry()
    {
        var subjectExpiry = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 2);
        var subject = CreateJwt(subjectExpiry);
        var key = Auth0OnBehalfOfTokenCache.Key(subject, Audience, "audit:read", null);
        var ct = TestContext.Current.CancellationToken;

        await _cache.GetTokenAsync(subject, Audience, "audit:read", token: ct);
        Assert.True((await _fusionCache.TryGetAsync<OnBehalfOfToken>(key, token: ct)).HasValue);

        var wait = subjectExpiry - DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(250);
        await Task.Delay(wait, ct);

        // The token issued by Auth0 lasts 600 seconds, so only the subject expiry can have evicted it.
        Assert.False((await _fusionCache.TryGetAsync<OnBehalfOfToken>(key, token: ct)).HasValue);
    }

    [Fact]
    public async Task ExpiredSubjectToken_Throws401_WithoutCallingAuth0()
    {
        var subject = CreateJwt(DateTimeOffset.UtcNow.AddMinutes(-1));

        var ex = await Assert.ThrowsAsync<Auth0OnBehalfOfException>(async () =>
            await _cache.GetTokenAsync(subject, Audience, "audit:read", token: TestContext.Current.CancellationToken));

        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("invalid_grant", ex.Error);
        AssertExchangeCount(0);
    }

    [Fact]
    public async Task OpaqueSubjectToken_IsExchanged()
    {
        var ct = TestContext.Current.CancellationToken;

        await _cache.GetTokenAsync("opaque-token", Audience, token: ct);
        await _cache.GetTokenAsync("opaque-token", Audience, token: ct);

        AssertExchangeCount(1);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "invalid_grant", "Subject token is invalid")]
    [InlineData(HttpStatusCode.Forbidden, "access_denied", "Client is not allowed to exchange tokens for this audience")]
    public async Task ErrorApiException_IsMapped(HttpStatusCode statusCode, string error, string description)
    {
        A.CallTo(() => _authClient.GetTokenOnBehalfOfAsync(A<OnBehalfOfTokenRequest>.Ignored, A<CancellationToken>.Ignored))
            .ThrowsAsync(new ErrorApiException(statusCode, new ApiError { Error = error, Message = description }));

        var subject = CreateJwt(DateTimeOffset.UtcNow.AddHours(1));
        var ct = TestContext.Current.CancellationToken;

        var ex = await Assert.ThrowsAsync<Auth0OnBehalfOfException>(async () => await _cache.GetTokenAsync(subject, Audience, token: ct));

        Assert.Equal((int)statusCode, ex.StatusCode);
        Assert.Equal(error, ex.Error);
        Assert.Equal(description, ex.ErrorDescription);
        Assert.Null(ex.RetryAfter);
        Assert.IsType<ErrorApiException>(ex.InnerException);

        // Failures are not cached.
        await Assert.ThrowsAsync<Auth0OnBehalfOfException>(async () => await _cache.GetTokenAsync(subject, Audience, token: ct));
        AssertExchangeCount(2);
    }

    [Fact]
    public async Task RateLimitApiException_IsMappedTo429WithRetryAfter()
    {
        using var response = new HttpResponseMessage((HttpStatusCode)429);
        response.Headers.TryAddWithoutValidation("x-ratelimit-limit", "10");
        response.Headers.TryAddWithoutValidation("x-ratelimit-remaining", "0");
        response.Headers.TryAddWithoutValidation("x-ratelimit-reset", DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds().ToString());
        var rateLimit = RateLimit.Parse(response.Headers);

        A.CallTo(() => _authClient.GetTokenOnBehalfOfAsync(A<OnBehalfOfTokenRequest>.Ignored, A<CancellationToken>.Ignored))
            .ThrowsAsync(new RateLimitApiException(rateLimit, new ApiError { Error = "too_many_requests", Message = "Rate limit exceeded" }));

        var ex = await Assert.ThrowsAsync<Auth0OnBehalfOfException>(async () =>
            await _cache.GetTokenAsync(CreateJwt(DateTimeOffset.UtcNow.AddHours(1)), Audience, token: TestContext.Current.CancellationToken));

        Assert.Equal(429, ex.StatusCode);
        Assert.Equal("too_many_requests", ex.Error);
        Assert.NotNull(ex.RetryAfter);
        Assert.InRange(ex.RetryAfter.Value, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
        Assert.IsType<RateLimitApiException>(ex.InnerException);
    }

    [Fact]
    public async Task Request_CarriesConfiguredCredentialsAndNormalizedParameters()
    {
        var subject = CreateJwt(DateTimeOffset.UtcNow.AddHours(1));

        await _cache.GetTokenAsync(subject, Audience, "audit:read  audit:export", "org_1", TestContext.Current.CancellationToken);

        A.CallTo(() => _authClient.GetTokenOnBehalfOfAsync(
                A<OnBehalfOfTokenRequest>.That.Matches(r =>
                    r.ClientId == ClientId &&
                    r.ClientSecret == ClientSecret &&
                    r.SubjectToken == subject &&
                    r.Audience == Audience &&
                    r.Scope == "audit:export audit:read" &&
                    r.Organization == "org_1"),
                A<CancellationToken>.Ignored))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CacheKey_DoesNotContainSubjectToken()
    {
        var keys = new ConcurrentBag<string>();
        _fusionCache.Events.Miss += (_, e) => keys.Add(e.Key);
        _fusionCache.Events.Set += (_, e) => keys.Add(e.Key);

        var subject = CreateJwt(DateTimeOffset.UtcNow.AddHours(1));
        await _cache.GetTokenAsync(subject, Audience, "audit:read", "org_1", TestContext.Current.CancellationToken);

        Assert.NotEmpty(keys);
        Assert.All(keys, key =>
        {
            Assert.Equal(Auth0OnBehalfOfTokenCache.Key(subject, Audience, "audit:read", "org_1"), key);
            Assert.DoesNotContain(subject, key);
            Assert.DoesNotContain(subject.Split('.')[1], key);
            Assert.DoesNotContain(Audience, key);
            Assert.DoesNotContain("org_1", key);
        });
    }

    [Fact]
    public void CacheKey_IsDeterministic_AndUrlSafe()
    {
        var key = Auth0OnBehalfOfTokenCache.Key("subject", Audience, "audit:read", "org_1");

        Assert.Equal(key, Auth0OnBehalfOfTokenCache.Key("subject", Audience, "audit:read", "org_1"));
        Assert.Matches($"^{nameof(Auth0OnBehalfOfTokenCache)}:[A-Za-z0-9_-]{{43}}$", key);
    }

    [Theory]
    [InlineData("a-b", "c", null, "a", "b-c", null)]
    [InlineData("a", "b-c", "d", "a", "b", "c-d")]
    [InlineData("a", "b", "c-d", "a-b", "c", "d")]
    public void CacheKey_DistinguishesValuesThatShareSeparators(
        string audience1, string scope1, string organization1,
        string audience2, string scope2, string organization2)
    {
        Assert.NotEqual(
            Auth0OnBehalfOfTokenCache.Key("subject", audience1, scope1, organization1),
            Auth0OnBehalfOfTokenCache.Key("subject", audience2, scope2, organization2));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("  ", "")]
    [InlineData("b a", "a b")]
    [InlineData("a\tb  a", "a b")]
    [InlineData("B a b", "B a b")]
    public void NormalizeScope_SplitsDeduplicatesAndSortsOrdinally(string scope, string expected)
    {
        Assert.Equal(expected, Auth0OnBehalfOfTokenCache.NormalizeScope(scope));
    }

    private static string CreateJwt(DateTimeOffset expiresAt, string subject = "auth0|user")
    {
        static string Encode(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var header = Encode("""{"alg":"RS256","typ":"JWT"}""");
        var payload = Encode($$"""{"sub":"{{subject}}","jti":"{{Guid.NewGuid()}}","exp":{{expiresAt.ToUnixTimeSeconds()}}}""");
        return $"{header}.{payload}.c2lnbmF0dXJl";
    }

    private sealed class SharedFusionCacheProvider(IFusionCache cache) : IFusionCacheProvider
    {
        public IFusionCache GetCache(string cacheName) => cache;

        public IFusionCache GetCacheOrNull(string cacheName) => cache;
    }
}
