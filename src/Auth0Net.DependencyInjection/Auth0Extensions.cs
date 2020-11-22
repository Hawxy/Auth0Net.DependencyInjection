using System;
using System.Net.Http;
using Auth0.AuthenticationApi;
using Auth0.ManagementApi;
using Auth0Net.DependencyInjection.Cache;
using Auth0Net.DependencyInjection.HttpClient;
using Auth0Net.DependencyInjection.Injectables;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for integrating Auth0 .NET with <see cref="IServiceCollection"/> and <see cref="IHttpClientBuilder"/>
    /// </summary>
    public static class Auth0Extensions
    {

        /// <summary>
        /// Adds an <see cref="AuthenticationApiClient" /> integrated with <see cref="IHttpClientBuilder" />. 
        /// </summary>
        /// <remarks>
        /// Use this lightweight integration if you're only using the <see cref="AuthenticationApiClient"/> and no other features of this library. 
        /// </remarks>
        /// <param name="services">The <see cref="IServiceCollection" />.</param>
        /// <param name="domain"></param>
        /// <returns>An <see cref="IHttpClientBuilder" /> that can be used to configure the <see cref="HttpClientAuthenticationConnection"/>.</returns>
        public static IHttpClientBuilder AddAuth0AuthenticationClientCore(this IServiceCollection services, string domain)
        {
            services.AddOptions<Auth0Configuration>().Validate(x => !string.IsNullOrWhiteSpace(x.Domain), "Auth0 Domain cannot be null or empty");
            services.Configure<Auth0Configuration>(x=> x.Domain = domain);

            services.AddScoped<InjectableAuthenticationApiClient>();
            services.AddScoped<AuthenticationApiClient>(x => x.GetRequiredService<InjectableAuthenticationApiClient>());

            return services.AddHttpClient<IAuthenticationConnection, HttpClientAuthenticationConnection>();
        }

        /// <summary>
        /// Adds a <see cref="AuthenticationApiClient" /> integrated with <see cref="IHttpClientBuilder" /> as well as the <see cref="IAuth0TokenCache" /> and related services to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <remarks>
        /// This configuration is required to use the <see cref="IHttpClientBuilder"/> and token caching integration.
        /// </remarks>
        /// <param name="services">The <see cref="IServiceCollection" />.</param>
        /// <param name="config">A delegate that is used to configure the instance of <see cref="Auth0Configuration" />.</param>
        /// <returns>An <see cref="IHttpClientBuilder" /> that can be used to configure the <see cref="HttpClientAuthenticationConnection"/>.</returns>
        public static IHttpClientBuilder AddAuth0AuthenticationClient(this IServiceCollection services, Action<Auth0Configuration> config)
        {
            services.AddOptions<Auth0Configuration>()
                .Validate(x => !string.IsNullOrWhiteSpace(x.ClientId) && !string.IsNullOrWhiteSpace(x.Domain) && !string.IsNullOrWhiteSpace(x.ClientSecret),
                    "Auth0 Configuration cannot have empty values");
            
            services.Configure(config);
            services.AddLazyCache();

            services.AddScoped<InjectableAuthenticationApiClient>();
            services.AddScoped<AuthenticationApiClient>(x => x.GetRequiredService<InjectableAuthenticationApiClient>());
            return services.AddHttpClient<IAuthenticationConnection, HttpClientAuthenticationConnection>();
        }
        /// <summary>
        /// Adds a <see cref="ManagementApiClient" /> integrated with <see cref="IHttpClientBuilder" /> to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <remarks>
        /// The domain used to construct the Management connection is the same as set in <see cref="AddAuth0AuthenticationClient"/>.
        /// </remarks>
        /// <param name="services">The <see cref="IServiceCollection" />.</param>
        /// <returns>An <see cref="IHttpClientBuilder" /> that can be used to configure the <see cref="HttpClientManagementConnection"/>.</returns>
        public static IHttpClientBuilder AddAuth0ManagementClient(this IServiceCollection services)
        {
            services.AddScoped<InjectableManagementApiClient>();
            services.AddScoped<ManagementApiClient>(resolver => resolver.GetRequiredService<InjectableManagementApiClient>());

            return services.AddHttpClient<IManagementConnection, HttpClientManagementConnection>();
        }

        /// <summary>
        /// Adds a <see cref="DelegatingHandler"/> to the <see cref="IHttpClientBuilder"/> that will automatically add a Auth0 Machine-to-Machine JWT token to the Authorization header.
        /// </summary>
        /// <remarks>
        /// If no audience is provided, the handler will use the <see cref="HttpRequestMessage"/>'s Absolute Uri as the audience.
        /// </remarks>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> you wish to configure. </param>
        /// <param name="config">A delegate that is used to configure the instance of <see cref="Auth0TokenConfig" />.</param>
        /// <returns>An <see cref="IHttpClientBuilder" /> that can be used to configure the <see cref="HttpClient"/>.</returns>
        public static IHttpClientBuilder AddTokenInjection(this IHttpClientBuilder builder, Action<Auth0TokenConfig>? config = null)
        {
            var c = new Auth0TokenConfig();
            config?.Invoke(c);

            builder.Services.TryAddScoped<IAuth0TokenCache, Auth0TokenCache>();
            return builder.AddHttpMessageHandler(provider => 
                new Auth0TokenHandler(provider.GetRequiredService<IAuth0TokenCache>(), c));
        }

        /// <summary>
        /// Adds a <see cref="DelegatingHandler"/> to the <see cref="IHttpClientBuilder"/> that will automatically add a Auth0 Management JWT token to the Authorization header.
        /// </summary>
        /// <remarks>
        /// The domain used to resolve the token is the same as set in <see cref="AddAuth0AuthenticationClient"/>
        /// </remarks>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> you wish to configure.</param>
        /// <returns>An <see cref="IHttpClientBuilder" /> that can be used to configure the <see cref="HttpClient"/>.</returns>
        public static IHttpClientBuilder AddManagementTokenInjection(this IHttpClientBuilder builder)
        {
            builder.Services.TryAddScoped<IAuth0TokenCache, Auth0TokenCache>();
            return builder.AddHttpMessageHandler(provider => 
                new Auth0TokenHandler(provider.GetRequiredService<IAuth0TokenCache>(), new Auth0TokenConfig(UriHelpers.GetValidManagementUri(provider.GetRequiredService<IOptionsSnapshot<Auth0Configuration>>().Value.Domain).ToString())));
        }
    }

}
