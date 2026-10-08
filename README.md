# Auth0.NET Dependency Injection Extensions
[![NuGet](https://img.shields.io/nuget/v/Auth0Net.DependencyInjection.svg?style=flat-square)](https://www.nuget.org/packages/Auth0Net.DependencyInjection)
[![Nuget](https://img.shields.io/nuget/dt/Auth0Net.DependencyInjection?style=flat-square)](https://www.nuget.org/packages/Auth0Net.DependencyInjection)

<h1 align="center">
<img align="center" src="https://user-images.githubusercontent.com/975824/128343470-8d97e39d-ff8a-4daf-8ebf-f9039a46abd6.png" height="130px" />
</h1>

Integrating [Auth0.NET](https://github.com/auth0/auth0.net) into your project whilst following idiomatic .NET conventions can be cumbersome and involve a sizable amount of boilerplate shared between projects. 

This library hopes to solve that problem, featuring:

 :white_check_mark: Extensions for `Microsoft.Extensions.DependencyInjection`.
 
 :white_check_mark: Automatic access token caching & renewal for the Management API and your own REST & Grpc services
 
 :white_check_mark: [HttpClientFactory](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests) integration for centralized extensibility and management of the internal HTTP handlers.
 
 :white_check_mark: `IHttpClientBuilder` extensions, providing handlers to automatically append access tokens to outgoing requests.

 :white_check_mark: Cached On-Behalf-Of token exchange, for calling other APIs as the current user.
 
 This library is compatible with .NET 8+ as well as .NET Framework 4.8 and is suitable for use in ASP.NET Core and standalone .NET Generic Host applications.
 
 ## Install
 
 Add `Auth0Net.DependencyInjection` to your project:
 
 ```
dotnet add package Auth0Net.DependencyInjection
```
 
 ## Scenarios
 
 ### Authentication Client Only
 
![Auth0Authentication](https://user-images.githubusercontent.com/975824/128319560-4b859296-44f5-4219-a1b3-8255bf29f1b3.png)
 
If you're simply using the `AuthenticationApiClient` and nothing else, you can call `AddAuth0AuthenticationClient` and pass in your Auth0 Domain. This integration is lightweight and does not support any other features of this library. 
 
 ```csharp
services.AddAuth0AuthenticationClient("your-auth0-domain.auth0.com");
```

You can then request the `IAuthenticationApiClient` within your class:

```csharp

public class AuthController : ControllerBase
{
    private readonly IAuthenticationApiClient _authenticationApiClient;

    public AuthController(IAuthenticationApiClient authenticationApiClient)
    {
        _authenticationApiClient = authenticationApiClient;
    }
 ```

### Authentication Client + Management Client 
 
![Auth0AuthenticationAndManagement](https://user-images.githubusercontent.com/975824/128319611-9083d473-191d-4593-ad0f-9669335dbb62.png)

 
Add the `AuthenticationApiClient` with `AddAuth0AuthenticationClient`, and provide a [machine-to-machine application](https://auth0.com/docs/applications/set-up-an-application/register-machine-to-machine-applications) configuration that will be consumed by the Management Client, Token Cache and IHttpClientBuilder integrations. This extension **must** be called before using any other extensions within this library:
 
 ```csharp
services.AddAuth0AuthenticationClient(config =>
{
    config.Domain = builder.Configuration["Auth0:Domain"];
    config.ClientId = builder.Configuration["Auth0:ClientId"];
    config.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
});
```

Add the `ManagementApiClient` with `AddAuth0ManagementClient()`. The client will attach the Access Token automatically:

```csharp
services.AddAuth0ManagementClient();
```

Ensure your Machine-to-Machine application is authorized to request tokens from the Managment API and it has the correct scopes for the features you wish to use.

You can then request the `IManagementApiClient` (or `IAuthenticationApiClient`) within your services:

```csharp
public class MyAuth0Service : IAuth0Service
{
    private readonly IManagementApiClient _managementApiClient;

    public MyAuth0Service(IManagementApiClient managementApiClient)
    {
        _managementApiClient = managementApiClient;
    }
 ```
 
 
#### Handling Custom Domains

If you're using a custom domain with your Auth0 tenant, and it's being specified when calling `AddAuth0AuthenticationClient`, you will run into a problem whereby the `audience` of the Management API is being incorrectly set. You can override this via the `Audience` property:

```cs
services.AddAuth0ManagementClient(c =>
    {
        // Set the audience to your default Auth0 domain.
        c.Audience = "my-tenant.au.auth0.com";
    });
```

### External HttpClient & Grpc Services (Machine-To-Machine Tokens)

![Auth0AuthenticationAll](https://user-images.githubusercontent.com/975824/128319653-418e0e72-2ddf-4d02-9544-1d60bd523321.png)

**Note:** This feature relies on `services.AddAuth0AuthenticationClient(config => ...)` being called and configured as outlined in the previous section. 

This library includes a delegating handler - effectively middleware for your HttpClient - that will append an access token to all outbound requests. This is useful for calling other services that are protected by Auth0. This integration requires your service implementation to use `IHttpClientFactory` as part of its registration. You can read more about it [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests)

#### HttpClient
Use `AddAccessToken` along with the required audience:

```csharp
services.AddHttpClient<MyHttpService>(x => x.BaseAddress = new Uri(builder.Configuration["MyHttpService:Url"]))
        .AddAccessToken(config => config.Audience = builder.Configuration["MyHttpService:Audience"]);
```

#### Grpc

This extension is compatible with any registration that returns a `IHttpClientBuilder`, thus it can be used with [Grpc's client factory](https://docs.microsoft.com/en-us/aspnet/core/grpc/clientfactory):

```csharp
services.AddGrpcClient<UserService.UserServiceClient>(x => x.Address = new Uri(builder.Configuration["MyGrpcService:Url"]))
        .AddAccessToken(config => config.Audience = builder.Configuration["MyGrpcService:Audience"]);
```

#### Advanced

`AddAccessToken` also has an option for passing in a func that can resolve the audience at runtime. This can be useful if your expected audiences always follow a pattern, or if you rely on service discovery, such as from [Steeltoe.NET](https://docs.steeltoe.io/api/v3/discovery/discovering-services.html):

```csharp
services.AddHttpClient<MyHttpService>(x=> x.BaseAddress = new Uri("https://MyServiceName/"))
        .AddServiceDiscovery()
        .AddAccessToken(config => config.AudienceResolver = request => request.RequestUri.GetLeftPart(UriPartial.Authority));
```

### M2M Organizations Support

This library includes support for [Machine-to-Machine (M2M) Access for Organizations](https://auth0.com/docs/manage-users/organizations/organizations-for-m2m-applications), including static and dynamic scenarios.
This feature is important if your internal or third-party services expect a token to be scoped to a specific Auth0 organization.

Orgs support must be enabled for your combination of client/api/org(s) before usage.

#### Static Organization

Clients that simply require a single organization for a specific client can do so via setting the `Organization` property when configuring the access token:

```csharp
builder.Services
    .AddGrpcClient<UserService.UserServiceClient>(x => x.Address = new Uri(builder.Configuration["MyApi:Url"]!))
    .AddAccessToken(config =>
    {
        config.Audience = builder.Configuration["MyApi:Audience"];
        config.Organization = builder.Configuration["MyApi:Organization"];
    });
```

#### Dynamic Organization via Request Metadata

If you already include org metadata as part of your network request or via the request options and would like to easily migrate to Org-scoped tokens, you can choose to resolve the organization at runtime via the `OrganizationResolver`:

```csharp
builder.Services
    .AddGrpcClient<UserService.UserServiceClient>(x => x.Address = new Uri(builder.Configuration["MyApi:Url"]!))
    .AddAccessToken(config =>
    {
        config.Audience = builder.Configuration["MyApi:Audience"];
        config.OrganizationResolver = x =>
            x.Headers.TryGetValues("org-id", out var values) 
                ? values.SingleOrDefault() 
                : null;
    });
```

#### Dynamic Organization via Client Scope (Experimental)

If your organization source is scoped to the usage of your service, such as an ASP.NET Core request, then you'll want the ability to freely set the Organization.
You can achieve this by injecting your client via `OrganizationScopeFactory<TClient>` and then creating an organization scope via `.CreateScope`:

```csharp
// Inject the factory around your remote client
private readonly OrganizationScopeFactory<UsersService> _scopeFactory;

public QueryUsersService(OrganizationScopeFactory<UsersService> scopeFactory)
{
    _scopeFactory = scopeFactory;
}

public async Task CreateUserAsync(User user, string orgId)
{
    // Create the scope so the MTM token is generated with the current OrgId    
    using var orgScope = _scopeFactory.CreateScope(orgId);
    await orgScope.Client.CreateUser(user, stoppingToken);
}
  
```

ALWAYS ensure you dispose of the scope when finished.

There's a few limitations if you're using this functionality, as it uses `AsyncLocal` internally:

- Never use multiple client scopes at the same time, either with the same or different client types. This will throw an exception.
- Never call any other client that utilizes `.AddAccessToken` within a client scope. This may cause the wrong Organization ID/Name being used for a given request.

If you have a use-case for either of these items, please open an issue with an example.

This functionality is marked as experimental, and you must `#pragma warning disable AUTH0_EXPERIMENTAL` to use it. 

### On-Behalf-Of Token Exchange

Auth0's On-Behalf-Of (OBO) token exchange lets your API exchange the access token it received from a user for an access token to another API. The new token keeps the user's identity (`sub`, `org_id`) and records your API as the actor (`act`).

**Prerequisite:** create a Custom API client in Auth0 that is linked to your API, and allow it to exchange tokens for the target API. Its credentials are separate from the Machine-to-Machine credentials used elsewhere in this library.

Register the exchange after any `AddAuth0AuthenticationClient` overload. The domain-only overload is enough if you don't need Machine-to-Machine tokens:

```csharp
services.AddAuth0AuthenticationClient(builder.Configuration["Auth0:Domain"]);

services.AddAuth0OnBehalfOf(config =>
{
    config.ClientId = builder.Configuration["Auth0:OnBehalfOf:ClientId"];
    config.ClientSecret = builder.Configuration["Auth0:OnBehalfOf:ClientSecret"];
    // Or authenticate with Private Key JWT instead of a secret:
    // config.ClientAssertionSecurityKey = new RsaSecurityKey(rsa);
    // config.ClientAssertionSecurityKeyAlgorithm = SecurityAlgorithms.RsaSha256;
});
```

Inject `IAuth0OnBehalfOfTokenCache` and pass it the user's access token. In ASP.NET Core, `JwtBearer` saves the incoming token by default, so it can be read with `GetTokenAsync("access_token")`:

```csharp
app.MapPost("/downstream-token", async (HttpContext ctx, IAuth0OnBehalfOfTokenCache tokenCache) =>
{
    var subjectToken = await ctx.GetTokenAsync("access_token");
    if (string.IsNullOrEmpty(subjectToken))
        return Results.Unauthorized();

    try
    {
        var token = await tokenCache.GetTokenAsync(
            subjectToken,
            audience: "https://downstream.example.com/",
            scope: "read:data",
            organization: ctx.User.FindFirstValue("org_id"),
            token: ctx.RequestAborted);

        return Results.Ok(new { token.AccessToken, token.ExpiresAt, token.Scope });
    }
    catch (Auth0OnBehalfOfException ex)
    {
        // 401: the user's token is invalid or expired. 403: the client, scope or organization is not allowed. 429: rate limited, see ex.RetryAfter.
        return Results.Problem(statusCode: ex.StatusCode, title: ex.Error, detail: ex.ErrorDescription);
    }
});
```

`OnBehalfOfToken.Scope` holds the scopes Auth0 granted, which may be narrower than the scopes requested. If the user's token has already expired, Auth0 is not called and a 401 `Auth0OnBehalfOfException` is thrown.

### Calling a downstream API on behalf of the user

`AddOnBehalfOfToken` adds a handler to an `HttpClient` that exchanges the user's access token and sets the exchanged token as the `Authorization` header of each request. It requires `AddAuth0OnBehalfOf`; resolving the client without it throws an `InvalidOperationException`.

```csharp
services.AddAuth0AuthenticationClient(builder.Configuration["Auth0:Domain"]);

services.AddAuth0OnBehalfOf(config =>
{
    config.ClientId = builder.Configuration["Auth0:OnBehalfOf:ClientId"];
    config.ClientSecret = builder.Configuration["Auth0:OnBehalfOf:ClientSecret"];
});

services.AddHttpContextAccessor();

services.AddHttpClient<MyClient>(x => x.BaseAddress = new Uri(builder.Configuration["MyHttpService:Url"]))
    .AddOnBehalfOfToken(o =>
    {
        o.Audience = builder.Configuration["MyHttpService:Audience"];
        o.Scope = "read:things";
        o.SubjectTokenResolver = (sp, _) =>
            sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request.Headers.Authorization.ToString() is { } h
            && h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? h["Bearer ".Length..]
                : null;
    });
```

`SubjectTokenResolver` receives the service provider the handler was created from, which is not the scope of the incoming request, so read request state through a singleton such as `IHttpContextAccessor`.

When there is no `HttpContext`, such as in a background job, set the user's token on the request instead. A token set this way takes precedence over `SubjectTokenResolver`:

```csharp
var request = new HttpRequestMessage(HttpMethod.Get, "things").SetSubjectToken(token);
var response = await httpClient.SendAsync(request);
```

`Audience`, `AudienceResolver`, `Organization` and `OrganizationResolver` behave as they do for `AddAccessToken`, and an organization set by a client scope (see [Dynamic Organization via Client Scope](#dynamic-organization-via-client-scope-experimental)) takes precedence over both.

Failures are not handled by the handler. They propagate from `SendAsync` as an `Auth0OnBehalfOfException`, including a 401 when no subject token is available.

## Additional Functionality

### Utility 

This library exposes a simple string extension, `ToHttpsUrl()`, that can be used to format the naked Auth0 domain sitting in your configuration into a proper URL.

This is identical to `https://{Configuration["Auth0:Domain"]}/` that you usually end up writing _somewhere_ in your `Program.cs`.

For example, formatting the domain for the JWT Authority:

```csharp
.AddJwtBearer(options =>
             {
                 // "my-tenant.auth0.com" -> "https://my-tenant.auth0.com/"
                 options.Authority = builder.Configuration["Auth0:Domain"].ToHttpsUrl();
                 //...
             });
 ```

## Internals

### Client Lifetimes

Both the authentication and authorization clients are registered as singletons and are suitable for injection into any other lifetime.

### Samples

Both a .NET Generic Host and ASP.NET Core examples are available in the [samples](https://github.com/Hawxy/Auth0Net.DependencyInjection/tree/main/samples) directory.

### Internal Cache

The `Auth0TokenCache` will cache a token for a given audience until at least 95% of the expiry time. If a request to the cache is made between 95% and 99% of expiry, the token will be refreshed in the background before expiry is reached.

An additional 1% of lifetime is removed to protect against clock drift between distributed systems.

In some situations you might want to request an access token from Auth0 manually. You can achieve this by injecting `IAuth0TokenCache` into a class and calling `GetTokenAsync` with the audience of the API you're requesting the token for.

An in-memory-only instance of [FusionCache](https://github.com/ZiggyCreatures/FusionCache) is used as the caching implementation. This instance is _named_ and will not impact other usages of FusionCache. 

If you want to use your own implementation of FusionCache, specify `FusionCacheResolver` when configurating the authentication client:

```csharp
services.AddAuth0AuthenticationClient(x =>
 {
     //...
     // Use the default FusionCache instance registered via `.AddFusionCache()`
     x.FusionCacheResolver = provider => provider.GetDefaultCache();
 });
```

`IAuth0OnBehalfOfTokenCache` uses the same FusionCache instance. Exchanged tokens are cached per user token, audience, set of scopes and organization; scope order and duplicates do not matter. The cache key is a SHA-256 hash of these values, so it does not contain the user's token. An exchanged token is cached until 99% of its lifetime has passed or the user's token expires, whichever comes first. Unlike Machine-to-Machine tokens, these entries are never refreshed in the background, and fail-safe is disabled for them, so a token is never served after the user's token has expired.

If `FusionCacheResolver` returns a cache with a distributed second level, exchanged tokens are also written to the distributed cache. They are short-lived user tokens, so make sure that cache is appropriately secured.

## Disclaimer

I am not affiliated with nor represent Auth0. All implementation issues regarding the underlying `ManagementApiClient` and `AuthenticationApiClient` should go to the official [Auth0.NET Respository](https://github.com/auth0/auth0.net).

### License notices

Icons used under the [MIT License](https://github.com/auth0/identicons/blob/master/LICENSE) from the [Identicons](https://github.com/auth0/identicons) pack.
