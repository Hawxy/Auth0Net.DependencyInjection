namespace Auth0Net.DependencyInjection.Organizations;

/// <summary>
/// Represents a scoped instance tied to a specific organization. This class ensures that
/// the organization context is properly managed and disposed of when no longer needed.
///
/// Must be created via <see cref="OrganizationScopeFactory{TClient}"/>
/// </summary>
/// <typeparam name="T">
/// The type of the client used within the organization scope.
/// </typeparam>
public sealed class OrganizationScope<T> : IDisposable where T : class
{
    private readonly HttpClientOrganizationAccessor _accessor;
    
    internal OrganizationScope(T client, HttpClientOrganizationAccessor accessor)
    {
        _accessor = accessor;
        Client = client;
    }

    /// <summary>
    /// Gets the client instance associated with the current organization scope.
    /// </summary>
    public T Client { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        _accessor.Organization = null;
    }
}