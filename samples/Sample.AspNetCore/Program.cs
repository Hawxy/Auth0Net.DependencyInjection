using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;
using Auth0Net.DependencyInjection;
using Auth0Net.DependencyInjection.HttpClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sample.AspNetCore.Protos;


var builder = WebApplication.CreateBuilder(args);

// An extension method is included to convert a naked auth0 domain (my-tenant.auth0.au.com) to the correct format (https://my-tenant-auth0.au.com/)
string domain = builder.Configuration["Auth0:Domain"]!.ToHttpsUrl();

// Protect your API with authentication as you normally would
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = domain;
        options.Audience = builder.Configuration["Auth0:Audience"];
    });

// We'll require all endpoints to be authorized by default
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// If you're just using the authentication client and nothing else, you can use this lightweight version instead.
// builder.Services.AddAuth0AuthenticationClientCore(domain);


// Adds the AuthenticationApiClient client and provides configuration to be consumed by the management client, token cache, and IHttpClientBuilder integrations
builder.Services.AddAuth0AuthenticationClient(config =>
{
    config.Domain = domain;
    config.ClientId = builder.Configuration["Auth0:ClientId"];
    config.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
});

// Adds the ManagementApiClient with automatic injection of the management token based on the configuration set above.
builder.Services.AddAuth0ManagementClient().AddManagementAccessToken();

builder.Services.AddGrpc();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<UsersService>();

app.MapGet("/users", async ([FromServices] IManagementApiClient client) =>
{
    var user = await client.Users.GetAllAsync(new GetUsersRequest(), new PaginationInfo());

    return user.Select(x => new Sample.AspNetCore.User(x.UserId, x.FullName, x.Email)).ToArray();
});


app.Run();