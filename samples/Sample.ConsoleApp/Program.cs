using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.ConsoleApp.Services;
using User;

namespace Sample.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(x=> x.AddUserSecrets("096fd757-814d-4d2a-a02e-ef4f19cdc656"))
                .ConfigureServices((context, collection) =>
                {
                    // Configure the authentication client and token cache, as we'll need it
                    collection.AddAuth0AuthenticationClient(config =>
                    {
                        config.Domain = context.Configuration["Auth0:Domain"];
                        config.ClientId = context.Configuration["Auth0:ClientId"];
                        config.ClientSecret = context.Configuration["Auth0:ClientSecret"];
                    });

                    // Automatically add the authorization header with our token on every outgoing request
                    collection
                        .AddHttpClient<UsersService>(x => x.BaseAddress = new Uri(context.Configuration["AspNetCore:Url"]))
                        .AddAccessToken(config => config.Audience = context.Configuration["AspNetCore:Audience"]);

                    // Works for the Grpc integration too!
                    collection
                        .AddGrpcClient<UserService.UserServiceClient>(x => x.Address = new Uri(context.Configuration["AspNetCore:Url"]))
                        .AddAccessToken(config => config.Audience = context.Configuration["AspNetCore:Audience"]);

                    collection.AddHostedService<PumpingBackgroundService>();

                });


            await hostBuilder.RunConsoleAsync();
        }
    }
}
