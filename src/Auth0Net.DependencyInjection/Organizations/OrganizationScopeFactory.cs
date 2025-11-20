using System.Diagnostics.CodeAnalysis;
using Auth0.AuthenticationApi;
using Auth0.ManagementApi;

namespace Auth0Net.DependencyInjection.Organizations;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TClient"></typeparam>
[Experimental("AUTH0_EXPERIMENTAL")]
public class OrganizationScopeFactory<TClient> where TClient: class
{
    private readonly TClient _client;
    private readonly HttpClientOrganizationAccessor _accessor;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="client"></param>
    /// <param name="accessor"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public OrganizationScopeFactory(TClient client, HttpClientOrganizationAccessor accessor)
    {
        if (client is IAuthenticationApiClient or IManagementApiClient)
        {
            throw new InvalidOperationException($"{nameof(OrganizationScopeFactory<TClient>)} is designed for use with your own remote clients and cannot be used with Auth0 client types.");
        }

        _client = client;
        _accessor = accessor;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="organization"></param>
    /// <returns></returns>
    public OrganizationScope<TClient> CreateScope(string organization)
    {
        _accessor.Organization = organization;
        return new OrganizationScope<TClient>(_client, _accessor);
    }

}