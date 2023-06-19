using Auth0Net.DependencyInjection;
using Auth0Net.DependencyInjection.HttpClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.AspNetCore.Protos;

namespace Sample.AspNetCore;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        // An extension method is included to convert a naked auth0 domain (my-tenant.auth0.au.com) to the correct format (https://my-tenant-auth0.au.com/)
        string domain = Configuration["Auth0:Domain"].ToHttpsUrl();

        // Equivalent to 
        // string domain = $"https://{Configuration["Auth0:Domain"]}/";

        // Protect your API with authentication as you normally would
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = domain;
                options.Audience = Configuration["Auth0:Audience"];
            });

        // We'll require all endpoints to be authorized by default
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        // If you're just using the authentication client and nothing else, you can use this lightweight version instead.
        // services.AddAuth0AuthenticationClientCore(domain);

        // Adds the AuthenticationApiClient client and provides configuration to be consumed by the management client, token cache, and IHttpClientBuilder integrations
        services.AddAuth0AuthenticationClient(config =>
        {
            config.Domain = domain;
            config.ClientId = Configuration["Auth0:ClientId"];
            config.ClientSecret = Configuration["Auth0:ClientSecret"];
        });

        // Adds the ManagementApiClient with automatic injection of the management token based on the configuration set above.
        services.AddAuth0ManagementClient().AddManagementAccessToken();

        services.AddGrpc();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGrpcService<UsersService>();
        });
    }
}