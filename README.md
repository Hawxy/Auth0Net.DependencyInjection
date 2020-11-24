# Auth0.NET Dependency Injection Extensions

<h1 align="center">
<img align="center" src="https://github.com/Hawxy/Auth0Net.DependencyInjection/blob/v1.0.0/src/Auth0Net.DependencyInjection/Images/icon.png" height="130px" />
</h1>

Integrating [Auth0.NET](https://github.com/auth0/auth0.net) into your project whilst attempting to follow idiomatic .NET Core conventions isn't exactly straight-forward, and can involve a sizable amount of boilerplate shared between projects. 

This library hopes to solve that problem, featuring:

 :white_check_mark: Extensions for `Microsoft.Extensions.DependencyInjection`
 
 :white_check_mark: Automatic access token caching for the Management API and your own REST & Grpc services
 
 :white_check_mark: [HttpClientFactory](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests) integration for correct management for the underlying connection
 
 :white_check_mark: `IHttpClientBuilder` extensions to append access tokens to outgoing requests
 
 This library supports .NET Core 3.1 & .NET 5.
 
 
 ## Install
 
 Add `Auth0Net.DependencyInjection` to your project from nuget:
 
 `Install-Package Auth0Net.DependencyInjection`
 
 ## Scenarios
 
 ### Authentication Client Only
 
![Auth0 Authentication](docs/images/Auth0Authentication.png?raw=true)
 
If you're simply using the `AuthenticationApiClient` (ie for URL building and requests driven by the user's access token), I've provided a lightweight integration:
 
 ```csharp
services.AddAuth0AuthenticationClientCore("your-auth0-domain.auth0.com");
```

### Authentication Client + Management Client 
 
![Auth0 Authentication & Management](docs/images/Auth0Authentication+Management.png?raw=true)
 
Add the `AuthenticationApiClient` with `AddAuth0AuthenticationClient`, and provide the configuration that will be consumed by the Management Client, Token Cache and IHttpClientBuilder integrations:
 
 ```csharp
services.AddAuth0AuthenticationClient(config =>
{
    config.Domain = Configuration["Auth0:Domain"];
    config.ClientId = Configuration["Auth0:ClientId"];
    config.ClientSecret = Configuration["Auth0:ClientSecret"];
});
```

Add the `ManagementApiClient` with `AddAuth0ManagementClient()` and add the `DelegatingHandler` with `AddManagementAccessToken()` that will attach the Access Token automatically:

```csharp
services.AddAuth0ManagementClient().AddManagementAccessToken();
```

### With HttpClient and/or Grpc Services

![Auth0 All](docs/images/Auth0All.png?raw=true)

**Note:** This feature relies on `services.AddAuth0AuthenticationClient(config => ...)` being called and configured as outlined in the previous example. 

If you wish to append machine-to-machine tokens to outbound requests from your HTTP services, you can use the `AddAccessToken` extension method along with your service's audience:

```csharp
services.AddHttpClient<MyHttpService>(x => x.BaseAddress = new Uri(context.Configuration["MyHttpService:Url"]))
        .AddAccessToken(config => config.Audience = context.Configuration["MyHttpService:Audience"]);
```

This extension method is compatible with any registration that returns a `IHttpClientBuilder`, thus it can be used with [Grpc's client factory](https://docs.microsoft.com/en-us/aspnet/core/grpc/clientfactory):

```csharp
services.AddGrpcClient<UserService.UserServiceClient>(x => x.Address = new Uri(context.Configuration["MyGrpcService:Url"]))
        .AddAccessToken(config => config.Audience = context.Configuration["MyGrpcService:Audience"]);
```

`AddAccessToken` also has an option for passing in a func that can resolve the audience at runtime. This can be useful if your audiences always follow a pattern, or if you rely on service discovery, such as from [Steeltoe.NET](https://docs.steeltoe.io/api/v3/discovery/discovering-services.html):

```csharp
services.AddHttpClient<MyHttpService>(x=> x.BaseAddress = new Uri("https://MyServiceName/"))
        .AddServiceDiscovery()
        .AddAccessToken(config => config.AudienceResolver = request => request.RequestUri.GetLeftPart(UriPartial.Authority));
```

## Disclaimer

I am not affiliated with nor represent Auth0. All implementation issues regarding the underlying `ManagementApiClient` and `AuthenticationApiClient` should go to the official [Auth0.NET Respository](https://github.com/auth0/auth0.net).

### License notice

Icons used under the [MIT License](https://github.com/auth0/identicons/blob/master/LICENSE) from the [Identicons](https://github.com/auth0/identicons) pack.

.NET Logo used under the [Creative Commons 4.0-Share Alike-By License](https://github.com/campusMVP/dotnetLogoPack/blob/main/License-CC-by-sa.md) from the [dotnetLogos](https://github.com/campusMVP/dotnetLogoPack) respository.
