using System;
using Auth0.AuthenticationApi;
using Auth0.ManagementApi;
using Auth0NET.DependencyInjection.Cache;
using Auth0Net.DependencyInjection.HttpClient;
using Auth0NET.DependencyInjection.HttpClient;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class Auth0Extensions
    {
        public static IHttpClientBuilder AddAuth0AuthenticationClient(this IServiceCollection collection, Action<Auth0TokenCacheConfiguration>? config = null)
        {
            collection.AddOptions<Auth0TokenCacheConfiguration>();

            if (config != null)
                collection.Configure(config);

            collection.AddLazyCache();
            collection.AddScoped(x => new AuthenticationApiClient(new Uri(x.GetRequiredService<IOptionsSnapshot<Auth0TokenCacheConfiguration>>().Value.Domain), x.GetRequiredService<IAuthenticationConnection>()));
            return collection.AddHttpClient<IAuthenticationConnection, HttpClientAuthenticationConnection>();
        }
        public static IHttpClientBuilder AddAuth0ManagementClient(this IServiceCollection collection, string domain)
        {
            collection.AddScoped(x => new ManagementApiClient(null, Auth0Util.GetAuth0ApiUrl(domain), x.GetRequiredService<IManagementConnection>()));
            return collection.AddHttpClient<IManagementConnection, HttpClientManagementConnection>();
        }

        public static IHttpClientBuilder AddMachineTokenInjection(this IHttpClientBuilder builder, Action<Auth0TokenConfig> config)
        {
            var c = new Auth0TokenConfig();
            config(c);

            builder.Services.TryAddScoped<IAuth0TokenCache, Auth0TokenCache>();
            builder.Services.TryAddTransient<Auth0TokenHandler>();
            builder.AddHttpMessageHandler(handler => new Auth0TokenHandler(handler.GetRequiredService<IAuth0TokenCache>(), c));
            return builder.AddHttpMessageHandler<Auth0TokenHandler>();
        }

        public static IHttpClientBuilder AddManagementTokenInjection(this IHttpClientBuilder builder)
        {
            builder.Services.TryAddScoped<IAuth0TokenCache, Auth0TokenCache>();
            builder.Services.TryAddTransient<Auth0TokenHandler>();
            builder.AddHttpMessageHandler(handler =>
            {
                var config = handler.GetRequiredService<IOptionsSnapshot<Auth0TokenCacheConfiguration>>().Value;
                return new Auth0TokenHandler(handler.GetRequiredService<IAuth0TokenCache>(), new Auth0TokenConfig(Auth0Util.GetAuth0ApiUrl(config.Domain)));
            });
            return builder.AddHttpMessageHandler<Auth0TokenHandler>();
        }
    }
}
