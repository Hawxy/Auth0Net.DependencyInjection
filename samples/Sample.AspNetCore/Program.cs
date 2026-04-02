using System.Security.Claims;
using Auth0.ManagementApi;
using Auth0Net.DependencyInjection;
using Auth0Net.DependencyInjection.HttpClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sample.AspNetCore.Protos;


var builder = WebApplication.CreateBuilder(args);

var domain = builder.Configuration["Auth0:Domain"];

// Protect your API with authentication as you normally would
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = domain!.ToHttpsUrl();
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
    config.Domain = domain!;
    config.ClientId = builder.Configuration["Auth0:ClientId"];
    config.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
});

// Adds the ManagementApiClient with automatic injection of the management token based on the configuration set above.
builder.Services.AddAuth0ManagementClient();

builder.Services.AddGrpc();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<UsersService>();

app.MapGet("/users", async ([FromServices] IManagementApiClient client) =>
{
    var user = await client.Users.ListAsync(new ListUsersRequestParameters() { });

    return user.CurrentPage.Select(x => new Sample.AspNetCore.User(x.UserId, x.Name, x.Email)).ToArray();
});

app.MapGet("/users/org-scoped", async ([FromServices] IManagementApiClient client, HttpContext context, ILogger<Program> logger) =>
{
    var orgId = context.User.FindFirstValue("org_id");
    if (!string.IsNullOrEmpty(orgId)) {
        logger.LogInformation("Found org {org}", orgId);
    }
    var user = await client.Users.ListAsync(new ListUsersRequestParameters() { });

    return user.CurrentPage.Select(x => new Sample.AspNetCore.User(x.UserId, x.Name, x.Email)).ToArray();
});


app.Run();