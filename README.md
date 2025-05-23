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
 
 This library is compatible with .NET 8+ as well as .NET Framework 4.8 and is suitable for use in ASP.NET Core and standalone .NET Generic Host applications.
 
 ## Install
 
 Add `Auth0Net.DependencyInjection` to your project:
 
 ```
dotnet add package Auth0Net.DependencyInjection
```
 
 ## Scenarios
 
 ### Authentication Client Only
 
![Auth0Authentication](https://user-images.githubusercontent.com/975824/128319560-4b859296-44f5-4219-a1b3-8255bf29f1b3.png)
 
If you're simply using the `AuthenticationApiClient` and nothing else, you can call `AddAuth0AuthenticationClientCore` and pass in your Auth0 Domain. This integration is lightweight and does not support any other features of this library. 
 
 ```csharp
services.AddAuth0AuthenticationClientCore("your-auth0-domain.auth0.com");
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

Add the `ManagementApiClient` with `AddAuth0ManagementClient()` and add the `DelegatingHandler` with `AddManagementAccessToken()` that will attach the Access Token automatically:

```csharp
services.AddAuth0ManagementClient().AddManagementAccessToken();
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

If you're using a custom domain with your Auth0 tenant, you may run into a problem whereby the `audience` of the Management API is being incorrectly set. You can override this via the `Audience` property:

```cs
services.AddAuth0ManagementClient()
    .AddManagementAccessToken(c =>
    {
        c.Audience = "my-tenant.au.auth0.com";
    });
```

### With HttpClient and/or Grpc Services (Machine-To-Machine tokens)

![Auth0AuthenticationAll](https://user-images.githubusercontent.com/975824/128319653-418e0e72-2ddf-4d02-9544-1d60bd523321.png)

**Note:** This feature relies on `services.AddAuth0AuthenticationClient(config => ...)` being called and configured as outlined in the previous scenario. 

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

## Additional Functionality

### Enhanced Resilience

The default rate-limit behaviour in Auth0.NET is suboptimal, as it uses random backoff rather than reading the rate limit headers returned by Auth0.
This package includes an additional `.AddAuth0RateLimitResilience()` extension that adds improved rate limit handling to the Auth0 clients.
If you're running into rate limit failures, I highly recommend adding this functionality:

```csharp
services.AddAuth0ManagementClient()
    .AddManagementAccessToken()
    .AddAuth0RateLimitResilience();
```

When a retry occurs, you should see a warning log similar to:

`Resilience event occurred. EventName: '"OnRetry"', Source: '"IManagementConnection-RateLimitRetry"/""/"Retry"', Operation Key: 'null', Result: '429'`

### Utility 

This library exposes a simple string extension, `ToHttpsUrl()`, that can be used to format the naked Auth0 domain sitting in your configuration into a proper URL.

This is identical to `https://{Configuration["Auth0:Domain"]}/` that you usually end up writing _somewhere_ in your `Startup.cs`.

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

Both a .NET Generic Host and ASP.NET Core example are available in the [samples](https://github.com/Hawxy/Auth0Net.DependencyInjection/tree/main/samples) directory.

### Internal Cache

The `Auth0TokenCache` will cache a token for a given audience until at least 95% of the expiry time. If a request to the cache is made between 95% and 99% of expiry, the token will be refreshed in the background before expiry is reached.

An additional 1% of lifetime is removed to protect against clock drift between distributed systems.

In some situations you might want to request an access token from Auth0 manually. You can achieve this by injecting `IAuth0TokenCache` into a class and calling `GetTokenAsync` with the audience of the API you're requesting the token for.

An in-memory-only instance of [FusionCache](https://github.com/ZiggyCreatures/FusionCache) is used as the caching implementation. This instance is _named_ and will not impact other usages of FusionCache.

## Disclaimer

I am not affiliated with nor represent Auth0. All implementation issues regarding the underlying `ManagementApiClient` and `AuthenticationApiClient` should go to the official [Auth0.NET Respository](https://github.com/auth0/auth0.net).

### License notices

Icons used under the [MIT License](https://github.com/auth0/identicons/blob/master/LICENSE) from the [Identicons](https://github.com/auth0/identicons) pack.
