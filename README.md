# Auth0.NET Dependency Injection Extensions
![.NET Core Build & Test](https://github.com/Hawxy/Auth0Net.DependencyInjection/workflows/.NET%20Core%20Build%20&%20Test/badge.svg)
[![NuGet](https://img.shields.io/nuget/v/Auth0Net.DependencyInjection.svg?style=flat-square)](https://www.nuget.org/packages/Auth0Net.DependencyInjection)

<h1 align="center">
<img align="center" src="src/Auth0Net.DependencyInjection/Images/icon.png" height="130px" />
</h1>

Integrating [Auth0.NET](https://github.com/auth0/auth0.net) into your project whilst attempting to follow idiomatic .NET Core conventions can be cumbersome and involve a sizable amount of boilerplate shared between projects. 

This library hopes to solve that problem, featuring:

 :white_check_mark: Extensions for `Microsoft.Extensions.DependencyInjection`.
 
 :white_check_mark: Automatic access token caching & renewal for the Management API and your own REST & Grpc services
 
 :white_check_mark: [HttpClientFactory](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests) integration for centralized extensibility and management of the internal HTTP handlers.
 
 :white_check_mark: `IHttpClientBuilder` extensions, providing handlers to automatically append access tokens to outgoing requests.
 
 This library supports .NET Core 3.1 & .NET 5, and is suitable for use in ASP.NET Core and standalone .NET Generic Host applications.
 
 ## Install
 
 Add `Auth0Net.DependencyInjection` to your project:
 
 ```ps
Install-Package Auth0Net.DependencyInjection
```
 
 ## Scenarios
 
 ### Authentication Client Only
 
![Auth0 Authentication](docs/images/Auth0Authentication.png?raw=true)
 
If you're simply using the `AuthenticationApiClient` and nothing else, you can call `AddAuth0AuthenticationClientCore` and pass in your Auth0 Domain. This integration is lightweight and does not support any other features of this library. 
 
 ```csharp
services.AddAuth0AuthenticationClientCore("your-auth0-domain.auth0.com");
```

### Authentication Client + Management Client 
 
![Auth0 Authentication & Management](docs/images/Auth0Authentication+Management.png?raw=true)
 
Add the `AuthenticationApiClient` with `AddAuth0AuthenticationClient`, and provide a [machine-to-machine application](https://auth0.com/docs/applications/set-up-an-application/register-machine-to-machine-applications) configuration that will be consumed by the Management Client, Token Cache and IHttpClientBuilder integrations. This extension **must** be called before using any other extensions within this library:
 
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

Ensure your Machine-to-Machine application is authorized to request tokens from the Managment API and it has the correct scopes for the features you wish to use.

### With HttpClient and/or Grpc Services

![Auth0 All](docs/images/Auth0All.png?raw=true)

**Note:** This feature relies on `services.AddAuth0AuthenticationClient(config => ...)` being called and configured as outlined in the previous scenario. 

If you wish to append machine-to-machine tokens to outbound requests from your HTTP services, you can use the `AddAccessToken` extension method along with the required audience:

```csharp
services.AddHttpClient<MyHttpService>(x => x.BaseAddress = new Uri(context.Configuration["MyHttpService:Url"]))
        .AddAccessToken(config => config.Audience = context.Configuration["MyHttpService:Audience"]);
```

This extension method is compatible with any registration that returns a `IHttpClientBuilder`, thus it can be used with [Grpc's client factory](https://docs.microsoft.com/en-us/aspnet/core/grpc/clientfactory):

```csharp
services.AddGrpcClient<UserService.UserServiceClient>(x => x.Address = new Uri(context.Configuration["MyGrpcService:Url"]))
        .AddAccessToken(config => config.Audience = context.Configuration["MyGrpcService:Audience"]);
```

`AddAccessToken` also has an option for passing in a func that can resolve the audience at runtime. This can be useful if your expected audiences always follow a pattern, or if you rely on service discovery, such as from [Steeltoe.NET](https://docs.steeltoe.io/api/v3/discovery/discovering-services.html):

```csharp
services.AddHttpClient<MyHttpService>(x=> x.BaseAddress = new Uri("https://MyServiceName/"))
        .AddServiceDiscovery()
        .AddAccessToken(config => config.AudienceResolver = request => request.RequestUri.GetLeftPart(UriPartial.Authority));
```

### Samples

Both a .NET Generic Host and ASP.NET Core example are available in the [samples](https://github.com/Hawxy/Auth0Net.DependencyInjection/tree/main/samples) directory.

### Internal Cache

The `Auth0TokenCache` will  a token for a given audience 5 minutes before expiry. 

In some situations you might want to request an access token from Auth0 manually. You can achieve this by injecting `IAuth0TokenCache` into a class and calling `GetTokenAsync` with the audience of the API you're requesting the token for.

### Utility 

This library exposes a simple string extension, `ToAuth0Uri()`, that can be used to format the Auth0 domain in the correct structure to be used by other ASP.NET functionality. 

For example, formatting the domain for the JWT Authority:

```csharp
.AddJwtBearer(options =>
             {
                 // "my-tenant.auth0.com" -> "https://my-tenant-auth0.com/"
                 options.Authority = Configuration["Auth0:Domain"].ToAuth0Uri();
                 //...
             });
 ```

## Disclaimer

I am not affiliated with nor represent Auth0. All implementation issues regarding the underlying `ManagementApiClient` and `AuthenticationApiClient` should go to the official [Auth0.NET Respository](https://github.com/auth0/auth0.net).

### License notices

Icons used under the [MIT License](https://github.com/auth0/identicons/blob/master/LICENSE) from the [Identicons](https://github.com/auth0/identicons) pack.

.NET Logo used under the [Creative Commons 4.0-Share Alike-By License](https://github.com/campusMVP/dotnetLogoPack/blob/main/License-CC-by-sa.md) from the [dotnetLogos](https://github.com/campusMVP/dotnetLogoPack) respository.
