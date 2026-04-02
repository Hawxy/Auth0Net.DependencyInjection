using System.Net.Http;
using Auth0.ManagementApi;
using Auth0Net.DependencyInjection.Cache;
using Auth0Net.DependencyInjection.HttpClient;
using Microsoft.Extensions.Options;

namespace Auth0Net.DependencyInjection.Factory;

internal sealed class ManagementClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<Auth0Configuration> _rootConfig;
    private readonly IOptions<Auth0ManagementClientConfiguration> _managementConfig;
    private readonly IAuth0TokenCache _cache;
    
    public const string Auth0ManagementApiClient = nameof(Auth0ManagementApiClient);

    public ManagementClientFactory(
        IHttpClientFactory httpClientFactory,
        IOptions<Auth0Configuration> rootConfig,
        IOptions<Auth0ManagementClientConfiguration> managementConfig,
        IAuth0TokenCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _rootConfig = rootConfig;
        _managementConfig = managementConfig;
        _cache = cache;
    }

    public ManagementClient Create()
    {
        var audience = _managementConfig.Value.Audience ?? _rootConfig.Value.Domain;

        var clientOptions = new ManagementClientOptions
        {
            Domain = _rootConfig.Value.Domain,
            HttpClient = _httpClientFactory.CreateClient(Auth0ManagementApiClient),
            TokenProvider = new Auth0ManagementTokenProvider(
                _cache,
                UriHelpers.GetValidManagementUri(audience).ToString()),
            MaxRetries = _managementConfig.Value.MaxRetries,
            Timeout = _managementConfig.Value.Timeout,
        };

        return new ManagementClient(clientOptions);
    }
}