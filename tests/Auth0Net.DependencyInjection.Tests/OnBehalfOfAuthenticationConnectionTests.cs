using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0Net.DependencyInjection.Injectables;
using FakeItEasy;
using Xunit;

namespace Auth0Net.DependencyInjection.Tests;

public class OnBehalfOfAuthenticationConnectionTests
{
    private readonly IAuthenticationConnection _inner = A.Fake<IAuthenticationConnection>();
    private IDictionary<string, string> _sentBody;

    public OnBehalfOfAuthenticationConnectionTests()
    {
        A.CallTo(() => _inner.SendAsync<AccessTokenResponse>(A<HttpMethod>.Ignored, A<Uri>.Ignored, A<object>.Ignored, A<IDictionary<string, string>>.Ignored, A<CancellationToken>.Ignored))
            .ReturnsLazily((HttpMethod _, Uri _, object body, IDictionary<string, string> _, CancellationToken _) =>
            {
                _sentBody = (IDictionary<string, string>)body;
                return Task.FromResult(new AccessTokenResponse { AccessToken = "token", ExpiresIn = 600 });
            });
    }

    private AuthenticationApiClient CreateClient() => new("hawxy.au.auth0.com", new OnBehalfOfAuthenticationConnection(_inner));

    [Fact]
    public async Task GetTokenOnBehalfOfAsync_Sends_RequestedTokenType()
    {
        await CreateClient().GetTokenOnBehalfOfAsync(new OnBehalfOfTokenRequest
        {
            SubjectToken = "subject",
            Audience = "https://downstream.example.com/",
            ClientId = "obo-id",
            ClientSecret = "obo-secret"
        });

        Assert.Equal(TokenType.AccessToken, _sentBody["requested_token_type"]);
        Assert.Equal(TokenType.AccessToken, _sentBody["subject_token_type"]);
    }

    [Fact]
    public async Task SendAsync_DoesNotOverwrite_ExistingRequestedTokenType()
    {
        var body = new Dictionary<string, string>
        {
            { "grant_type", "urn:ietf:params:oauth:grant-type:token-exchange" },
            { "requested_token_type", "urn:example:custom" }
        };

        await new OnBehalfOfAuthenticationConnection(_inner).SendAsync<AccessTokenResponse>(HttpMethod.Post, new Uri("https://hawxy.au.auth0.com/oauth/token"), body);

        Assert.Equal("urn:example:custom", _sentBody["requested_token_type"]);
    }

    [Fact]
    public async Task SendAsync_Ignores_OtherGrants()
    {
        await CreateClient().GetTokenAsync(new ClientCredentialsTokenRequest
        {
            Audience = "https://downstream.example.com/",
            ClientId = "client-id",
            ClientSecret = "client-secret"
        });

        Assert.False(_sentBody.ContainsKey("requested_token_type"));
    }
}
