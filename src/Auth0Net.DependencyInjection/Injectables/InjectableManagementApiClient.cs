﻿using Auth0.ManagementApi;
using Auth0Net.DependencyInjection.Cache;
using Auth0Net.DependencyInjection.HttpClient;
using Microsoft.Extensions.Options;

namespace Auth0Net.DependencyInjection.Injectables;

internal sealed class InjectableManagementApiClient : ManagementApiClient
{
    public InjectableManagementApiClient(IOptions<Auth0Configuration> config, IManagementConnection managementConnection)
        : base(null, UriHelpers.GetValidManagementUri(config.Value.Domain), managementConnection)
    {
    }
}