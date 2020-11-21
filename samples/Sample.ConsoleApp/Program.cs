using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sample.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, collection) =>
                {
                    collection.AddAuth0AuthenticationClient(config =>
                    {
                        config.Domain = context.Configuration["Auth0:Domain"];
                        config.ClientId = context.Configuration["Auth0:ClientId"];
                        config.ClientSecret = context.Configuration["Auth0:ClientSecret"];
                    });
                    collection.AddAuth0ManagementClient();
                });
        }
    }
}
