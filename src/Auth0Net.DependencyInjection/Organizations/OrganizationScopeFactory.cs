using System.Diagnostics.CodeAnalysis;
using Auth0.AuthenticationApi;
using Auth0.ManagementApi;

namespace Auth0Net.DependencyInjection.Organizations;

/// <summary>
/// Factory class for creating scoped instances of <see cref="OrganizationScope{TClient}"/>
/// associated with a specified organization.
/// </summary>
/// <typeparam name="TClient">
/// The type of client used within the organization scope. This must be a user-defined remote client.
/// </typeparam>
[Experimental("AUTH0_EXPERIMENTAL")]
public class OrganizationScopeFactory<TClient> where TClient: class
{
    private readonly TClient _client;
    private readonly HttpClientOrganizationAccessor _accessor;

    /// <summary>
    /// Factory for creating instances of <see cref="OrganizationScope{TClient}"/> for a specified organization.
    /// This factory is designed for use with custom remote clients and cannot be used with Auth0 client types like <see cref="Auth0.AuthenticationApi.IAuthenticationApiClient"/>
    /// or <see cref="Auth0.ManagementApi.IManagementApiClient"/>.
    /// </summary>
    /// <typeparam name="TClient">
    /// The type of the client used in the organization scope. 
    /// </typeparam>
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
    /// Creates a new instance of <see cref="OrganizationScope{TClient}"/> associated with the specified organization.
    /// This method sets the organization context for the scoped instance.
    /// </summary>
    /// <param name="organization">
    /// The identifier of the organization to associate with the created scope.
    /// </param>
    /// <returns>
    /// A new instance of <see cref="OrganizationScope{TClient}"/> linked to the specified organization.
    /// </returns>
    public OrganizationScope<TClient> CreateScope(string organization)
    {
        _accessor.Organization = organization;
        return new OrganizationScope<TClient>(_client, _accessor);
    }

}