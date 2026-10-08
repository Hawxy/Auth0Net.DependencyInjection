using System.Security.Claims;
using Auth0.ManagementApi;
using Auth0Net.DependencyInjection;
using Auth0Net.DependencyInjection.Cache;
using Auth0Net.DependencyInjection.HttpClient;
using Microsoft.AspNetCore.Authentication;
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

// Adds On-Behalf-Of token exchange, authenticating as the Custom API client linked to this API.
builder.Services.AddAuth0OnBehalfOf(config =>
{
    config.ClientId = builder.Configuration["Auth0:OnBehalfOf:ClientId"];
    config.ClientSecret = builder.Configuration["Auth0:OnBehalfOf:ClientSecret"];
});

// Calls another API with a token exchanged from the caller's access token.
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("Downstream", x => x.BaseAddress = new Uri(builder.Configuration["Auth0:OnBehalfOf:Url"]!))
    .AddOnBehalfOfToken(config =>
    {
        config.Audience = builder.Configuration["Auth0:OnBehalfOf:Audience"];
        config.Scope = "read:data";
        config.SubjectTokenResolver = (sp, _) =>
            sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request.Headers.Authorization.ToString() is { } h
            && h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? h["Bearer ".Length..]
                : null;
    });

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

// Exchanges the caller's access token for a token to another API, issued on the caller's behalf.
app.MapPost("/on-behalf-of/token", async (HttpContext context, IAuth0OnBehalfOfTokenCache tokenCache, IConfiguration configuration) =>
{
    // JwtBearer saves the incoming token by default.
    var subjectToken = await context.GetTokenAsync("access_token");
    if (string.IsNullOrEmpty(subjectToken))
        return Results.Unauthorized();

    try
    {
        var token = await tokenCache.GetTokenAsync(
            subjectToken,
            configuration["Auth0:OnBehalfOf:Audience"]!,
            scope: "read:data",
            organization: context.User.FindFirstValue("org_id"),
            token: context.RequestAborted);

        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new { access_token = token.AccessToken, expires_at = token.ExpiresAt, scope = token.Scope });
    }
    catch (Auth0OnBehalfOfException ex)
    {
        if (ex.RetryAfter is { } retryAfter)
            context.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

        return Results.Problem(statusCode: ex.StatusCode, title: ex.Error, detail: ex.ErrorDescription);
    }
});

// Calls another API on the caller's behalf, using the "Downstream" client registered above.
app.MapGet("/on-behalf-of/data", async (IHttpClientFactory factory, HttpContext context) =>
{
    var client = factory.CreateClient("Downstream");

    try
    {
        var data = await client.GetStringAsync("data", context.RequestAborted);
        return Results.Text(data, "application/json");
    }
    catch (Auth0OnBehalfOfException ex)
    {
        return Results.Problem(statusCode: ex.StatusCode, title: ex.Error, detail: ex.ErrorDescription);
    }
});

app.Run();