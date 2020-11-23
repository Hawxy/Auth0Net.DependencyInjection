# Auth0.NET Dependency Injection Extensions

Integrating [Auth0.NET](https://github.com/auth0/auth0.net) into your project whilst attempting to follow idiomatic .NET Core conventions isn't exactly straight-forward, and can involve a sizable amount of boilerplate shared between projects. 

This library hopes to solve that problem, featuring:

 :white_check_mark: Extensions for `Microsoft.Extensions.DependencyInjection`
 
 :white_check_mark: Automatic access token caching for the Management API and your own REST & Grpc services
 
 :white_check_mark: [HttpClientFactory](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests) integration with handlers to append JWTs to outgoing requests.
 
 This library supports .NET Core 3.1 & .NET 5.
 
 
 ## Typical Setup
 
 Install `Auth0Net.DependencyInjection` from Nuget into your .NET Core application.
 
 Add the `AuthenticationApiClient`, and provide the configuration that will be consumed by the Management Client, Token Cache and IHttpClientBuilder integrations:
 
 ```csharp
services.AddAuth0AuthenticationClient(config =>
{
    config.Domain = Configuration["Auth0:Domain"];
    config.ClientId = Configuration["Auth0:ClientId"];
    config.ClientSecret = Configuration["Auth0:ClientSecret"];
});
```

Add the `ManagementApiClient`, along with with the `DelegatingHandler` that will inject the Access Token automatically:

```csharp
services.AddAuth0ManagementClient().AddManagementTokenInjection();
```

## Disclaimer

I am not affiliated with nor represent Auth0. All implementation issues regarding the underlying `ManagementApiClient` and `AuthenticationApiClient` should go to the official [Auth0.NET Respository](https://github.com/auth0/auth0.net).

### Package Logo

Auth0 Logo used under the [MIT License](https://github.com/auth0/identicons/blob/master/LICENSE) from the [Identicons](https://github.com/auth0/identicons) pack.
.NET Logo used under the [Creative Commons 4.0-Share Alike-By License](https://github.com/campusMVP/dotnetLogoPack/blob/main/License-CC-by-sa.md) from the [dotnetLogos](https://github.com/campusMVP/dotnetLogoPack) respository.
