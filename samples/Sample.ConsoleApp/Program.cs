using Auth0Net.DependencyInjection;
using Sample.ConsoleApp;
using Sample.ConsoleApp.Services;
using User;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Configure the authentication client and token cache, as we'll need it
builder.Services.AddAuth0AuthenticationClient(config =>
{
    config.Domain = builder.Configuration["Auth0:Domain"]!;
    config.ClientId = builder.Configuration["Auth0:ClientId"];
    config.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
});

// Automatically add the authorization header with our token on every outgoing request
builder.Services 
    .AddHttpClient<UsersService>(x => x.BaseAddress = new Uri(builder.Configuration["AspNetCore:Url"]!))
    .AddAccessToken(config => config.Audience = builder.Configuration["AspNetCore:Audience"]);


// Works for the Grpc integration too!
builder.Services
    .AddGrpcClient<UserService.UserServiceClient>(x => x.Address = new Uri(builder.Configuration["AspNetCore:Url"]!))
    .AddAccessToken(config => config.Audience = builder.Configuration["AspNetCore:Audience"]);


builder.Services.AddHostedService<PumpingBackgroundService>();

var host = builder.Build();

await host.RunAsync();