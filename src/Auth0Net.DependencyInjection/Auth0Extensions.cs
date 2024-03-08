using Auth0.AuthenticationApi;
using Auth0.ManagementApi;
using Auth0Net.DependencyInjection.Cache;
using Auth0Net.DependencyInjection.HttpClient;
using Auth0Net.DependencyInjection.Injectables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Auth0Net.DependencyInjection;

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
    /// <param name="domain">The root domain for your Auth0 tenant.</param>
    /// <returns>An <see cref="IHttpClientBuilder" /> that can be used to configure the <see cref="HttpClientAuthenticationConnection"/>.</returns>
    public static IHttpClientBuilder AddAuth0AuthenticationClientCore(this IServiceCollection services, string domain)
    {
        if (services.Any(x => x.ServiceType == typeof(IAuthenticationApiClient)))
            throw new InvalidOperationException("AuthenticationApiClient has already been registered!");

        services.AddOptions<Auth0Configuration>()
            .Configure(x => x.Domain = domain)
            .Validate(x => !string.IsNullOrWhiteSpace(x.Domain), "Auth0 Domain cannot be null or empty");

        return services.AddAuth0AuthenticationClientInternal();
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
        if (services.Any(x => x.ServiceType == typeof(IAuthenticationApiClient)))
            throw new InvalidOperationException("AuthenticationApiClient has already been registered!");

        services.AddOptions<Auth0Configuration>()
            .Configure(config)
            .Validate(x => !string.IsNullOrWhiteSpace(x.ClientId) && !string.IsNullOrWhiteSpace(x.Domain) && !string.IsNullOrWhiteSpace(x.ClientSecret),
                "Auth0 Configuration cannot have empty values");

        return services.AddAuth0AuthenticationClientInternal(true);
    }


    /// <summary>
    /// Adds a <see cref="AuthenticationApiClient" /> integrated with <see cref="IHttpClientBuilder" /> as well as the <see cref="IAuth0TokenCache" /> and related services to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <remarks>
    /// This configuration is required to use the <see cref="IHttpClientBuilder"/> and token caching integration.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <param name="config">A delegate that is used to configure the instance of <see cref="Auth0Configuration" />, with the ability to request services from the <see cref="IServiceProvider"/></param>
    /// <returns>An <see cref="IHttpClientBuilder" /> that can be used to configure the <see cref="HttpClientAuthenticationConnection"/>.</returns>
    public static IHttpClientBuilder AddAuth0AuthenticationClient(this IServiceCollection services, Action<Auth0Configuration, IServiceProvider> config)
    {
        if (services.Any(x => x.ServiceType == typeof(IAuthenticationApiClient)))
            throw new InvalidOperationException("AuthenticationApiClient has already been registered!");

        services.AddOptions<Auth0Configuration>()
            .Configure(config)
            .Validate(x => !string.IsNullOrWhiteSpace(x.ClientId) && !string.IsNullOrWhiteSpace(x.Domain) && !string.IsNullOrWhiteSpace(x.ClientSecret),
                "Auth0 Configuration cannot have empty values");

        return services.AddAuth0AuthenticationClientInternal(true);
    }

    private static IHttpClientBuilder AddAuth0AuthenticationClientInternal(this IServiceCollection services,
        bool withCache = false)
    {
        if (withCache)
        {
            services.AddFusionCache(Constants.FusionCacheInstance);
            services.AddSingleton<IAuth0TokenCache, Auth0TokenCache>();
        }

        services.AddSingleton<IAuthenticationApiClient, InjectableAuthenticationApiClient>();
        return services.AddHttpClient<IAuthenticationConnection, HttpClientAuthenticationConnection>()
#if !NETSTANDARD2_0
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler()
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                })
#endif
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
    }



    /// <summary>
    /// Adds a <see cref="ManagementApiClient" /> integrated with <see cref="IHttpClientBuilder" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <remarks>
    /// The domain used to construct the Management connection is the same as set in <see cref="AddAuth0AuthenticationClient(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{Auth0Net.DependencyInjection.Cache.Auth0Configuration})"/>.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <returns>An <see cref="IHttpClientBuilder" /> that can be used to configure the <see cref="HttpClientManagementConnection"/>.</returns>
    public static IHttpClientBuilder AddAuth0ManagementClient(this IServiceCollection services)
    {
        services.AddSingleton<IManagementApiClient, InjectableManagementApiClient>();

        return services.AddHttpClient<IManagementConnection, HttpClientManagementConnection>()
#if !NETSTANDARD2_0
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler()
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                })
#endif
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Adds a <see cref="DelegatingHandler"/> to the <see cref="IHttpClientBuilder"/> that will automatically add a Auth0 Machine-to-Machine Access Token to the Authorization header.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/> you wish to configure. </param>
    /// <param name="config">A delegate that is used to configure the instance of <see cref="Auth0TokenHandlerConfig" />.</param>
    /// <returns>An <see cref="IHttpClientBuilder" /> that can be used to configure the <see cref="HttpClient"/>.</returns>
    public static IHttpClientBuilder AddAccessToken(this IHttpClientBuilder builder, Action<Auth0TokenHandlerConfig> config)
    {
        var c = new Auth0TokenHandlerConfig();
        config.Invoke(c);

        if (c.AudienceResolver is null && string.IsNullOrWhiteSpace(c.Audience))
            throw new ArgumentException("Audience or AudienceResolver must be set");

        return builder.AddHttpMessageHandler(provider =>
            new Auth0TokenHandler(provider.GetRequiredService<IAuth0TokenCache>(), c));
    }

    /// <summary>
    /// Adds a <see cref="DelegatingHandler"/> to the <see cref="IHttpClientBuilder"/> that will automatically add a Auth0 Management Access Token token to the Authorization header.
    /// </summary>
    /// <remarks>
    /// The domain used to resolve the token is the same as set in <see cref="AddAuth0AuthenticationClient(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{Auth0Net.DependencyInjection.Cache.Auth0Configuration})"/>, unless overriden.
    /// </remarks>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/> you wish to configure.</param>
    /// <param name="config">Additional configuration for the management client for custom domain scenarios.</param>
    /// <returns>An <see cref="IHttpClientBuilder" /> that can be used to configure the <see cref="HttpClient"/>.</returns>
    public static IHttpClientBuilder AddManagementAccessToken(this IHttpClientBuilder builder, Action<Auth0ManagementTokenConfiguration>? config = null)
    {
        var c = new Auth0ManagementTokenConfiguration();
        config?.Invoke(c);

        return builder.AddHttpMessageHandler(p =>
            new Auth0ManagementTokenHandler(
                p.GetRequiredService<IAuth0TokenCache>(),
                p.GetRequiredService<IOptions<Auth0Configuration>>(), c));
    }
}