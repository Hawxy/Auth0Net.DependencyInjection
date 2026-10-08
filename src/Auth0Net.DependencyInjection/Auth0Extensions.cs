using Auth0.AuthenticationApi;
using Auth0.ManagementApi;
using Auth0Net.DependencyInjection.Cache;
using Auth0Net.DependencyInjection.Factory;
using Auth0Net.DependencyInjection.HttpClient;
using Auth0Net.DependencyInjection.Injectables;
using Auth0Net.DependencyInjection.Organizations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    public static IHttpClientBuilder AddAuth0AuthenticationClient(this IServiceCollection services, string domain)
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
            services.AddAuth0FusionCache();
            services.AddSingleton<IAuth0TokenCache, Auth0TokenCache>();
        }

        services.AddSingleton<HttpClientOrganizationAccessor>();
        services.AddTransient(typeof(OrganizationScopeFactory<>));
        services.AddSingleton<IAuthenticationApiClient, InjectableAuthenticationApiClient>();
        return services.AddHttpClient<IAuthenticationConnection, HttpClientAuthenticationConnection>()
#if NET8_0
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler()
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                })
        .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
#endif
            ;

    }

    private static void AddAuth0FusionCache(this IServiceCollection services)
    {
        if (services.Any(x => x.ServiceType == typeof(Auth0FusionCacheMarker)))
            return;

        services.AddSingleton<Auth0FusionCacheMarker>();
        services.AddFusionCache(Constants.FusionCacheInstance);
    }

    /// <summary>
    /// Adds the <see cref="IAuth0OnBehalfOfTokenCache"/>, which exchanges a user's access token for an access token to another API using Auth0 On-Behalf-Of token exchange.
    /// </summary>
    /// <remarks>
    /// <see cref="AddAuth0AuthenticationClient(IServiceCollection,string)"/>, or another overload, must also be called. The domain-only overload is sufficient.
    /// The credentials are those of the Auth0 Custom API client linked to this API, and are separate from the Machine-to-Machine credentials in <see cref="Auth0Configuration"/>.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <param name="config">A delegate that is used to configure the instance of <see cref="Auth0OnBehalfOfConfiguration" />.</param>
    /// <returns>The <see cref="IServiceCollection" />.</returns>
    public static IServiceCollection AddAuth0OnBehalfOf(this IServiceCollection services, Action<Auth0OnBehalfOfConfiguration> config)
    {
        services.AddAuth0OnBehalfOfInternal().Configure(config);
        return services;
    }

    /// <summary>
    /// Adds the <see cref="IAuth0OnBehalfOfTokenCache"/>, which exchanges a user's access token for an access token to another API using Auth0 On-Behalf-Of token exchange.
    /// </summary>
    /// <remarks>
    /// <see cref="AddAuth0AuthenticationClient(IServiceCollection,string)"/>, or another overload, must also be called. The domain-only overload is sufficient.
    /// The credentials are those of the Auth0 Custom API client linked to this API, and are separate from the Machine-to-Machine credentials in <see cref="Auth0Configuration"/>.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <param name="config">A delegate that is used to configure the instance of <see cref="Auth0OnBehalfOfConfiguration" />, with the ability to request services from the <see cref="IServiceProvider"/>.</param>
    /// <returns>The <see cref="IServiceCollection" />.</returns>
    public static IServiceCollection AddAuth0OnBehalfOf(this IServiceCollection services, Action<Auth0OnBehalfOfConfiguration, IServiceProvider> config)
    {
        services.AddAuth0OnBehalfOfInternal().Configure(config);
        return services;
    }

    private static OptionsBuilder<Auth0OnBehalfOfConfiguration> AddAuth0OnBehalfOfInternal(this IServiceCollection services)
    {
        services.AddAuth0FusionCache();
        services.TryAddSingleton<IAuth0OnBehalfOfTokenCache>(provider =>
        {
            var client = provider.GetService<IAuthenticationApiClient>()
                ?? throw new InvalidOperationException($"No {nameof(IAuthenticationApiClient)} has been registered. Call {nameof(AddAuth0AuthenticationClient)} to use {nameof(AddAuth0OnBehalfOf)}.");

            return ActivatorUtilities.CreateInstance<Auth0OnBehalfOfTokenCache>(provider, client);
        });

        return services.AddOptions<Auth0OnBehalfOfConfiguration>()
            .Validate(x => x.IsValid(),
                "Auth0 On-Behalf-Of configuration requires a ClientId, and either a ClientSecret or a ClientAssertionSecurityKey with a ClientAssertionSecurityKeyAlgorithm");
    }

    /// <summary>
    /// Adds a <see cref="ManagementApiClient" /> integrated with <see cref="IHttpClientBuilder" /> to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <remarks>
    /// The domain used to construct the Management connection is the same as set in <see cref="AddAuth0AuthenticationClient(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{Auth0Net.DependencyInjection.Cache.Auth0Configuration})"/>.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <returns>An <see cref="IHttpClientBuilder" /> that can be used to configure the <see cref="ManagementClient"/>.</returns>
    public static IHttpClientBuilder AddAuth0ManagementClient(this IServiceCollection services, Action<Auth0ManagementClientConfiguration, IServiceProvider>? config = null)
    {
        var httpClientBuilder = services.AddHttpClient(ManagementClientFactory.Auth0ManagementApiClient)
#if NET8_0
            .ConfigurePrimaryHttpMessageHandler(() =>
                new SocketsHttpHandler()
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2)
                })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan)
#endif
            ;

        var optionsBuilder = services.AddOptions<Auth0ManagementClientConfiguration>();
        
        if(config != null)
            optionsBuilder.Configure(config);

        services.AddSingleton<ManagementClientFactory>();
        
        services.AddSingleton<IManagementApiClient>(sp => sp.GetRequiredService<ManagementClientFactory>().Create());

        return httpClientBuilder;
    }

    /// <summary>
    /// Adds a <see cref="System.Net.Http.DelegatingHandler"/> to the <see cref="IHttpClientBuilder"/> that will automatically add a Auth0 Machine-to-Machine Access Token to the Authorization header.
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
            new Auth0TokenHandler(provider.GetRequiredService<IAuth0TokenCache>(), c, provider.GetRequiredService<HttpClientOrganizationAccessor>()));
    }

    /// <summary>
    /// Adds a <see cref="System.Net.Http.DelegatingHandler"/> to the <see cref="IHttpClientBuilder"/> that exchanges the user's access token using Auth0 On-Behalf-Of token exchange,
    /// and adds the exchanged token to the Authorization header.
    /// </summary>
    /// <remarks>
    /// <see cref="AddAuth0OnBehalfOf(IServiceCollection,Action{Auth0OnBehalfOfConfiguration})"/>, or its other overload, must be called.
    /// The user's access token is taken from <see cref="OnBehalfOfRequestExtensions.SetSubjectToken"/> if set on the request, otherwise from <see cref="Auth0OnBehalfOfTokenHandlerConfig.SubjectTokenResolver"/>.
    /// Rejected exchanges are thrown from the request as an <see cref="Auth0OnBehalfOfException"/>. Other failures calling Auth0 propagate unchanged.
    /// </remarks>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/> you wish to configure. </param>
    /// <param name="config">A delegate that is used to configure the instance of <see cref="Auth0OnBehalfOfTokenHandlerConfig" />.</param>
    /// <returns>An <see cref="IHttpClientBuilder" /> that can be used to configure the <see cref="HttpClient"/>.</returns>
    /// <exception cref="ArgumentException">Neither an audience nor an audience resolver is set.</exception>
    public static IHttpClientBuilder AddOnBehalfOfToken(this IHttpClientBuilder builder, Action<Auth0OnBehalfOfTokenHandlerConfig> config)
    {
        var c = new Auth0OnBehalfOfTokenHandlerConfig();
        config.Invoke(c);

        if (c.AudienceResolver is null && string.IsNullOrWhiteSpace(c.Audience))
            throw new ArgumentException("Audience or AudienceResolver must be set");

        return builder.AddHttpMessageHandler(provider =>
        {
            var cache = provider.GetService<IAuth0OnBehalfOfTokenCache>()
                ?? throw new InvalidOperationException($"No {nameof(IAuth0OnBehalfOfTokenCache)} has been registered. Call {nameof(AddAuth0OnBehalfOf)} before using {nameof(AddOnBehalfOfToken)}.");

            return new Auth0OnBehalfOfTokenHandler(cache, c, provider.GetRequiredService<HttpClientOrganizationAccessor>(), provider);
        });
    }
}