namespace Auth0Net.DependencyInjection.Organizations;

public class OrganizationScope<T> : IDisposable where T: class
{
    private readonly HttpClientOrganizationAccessor _accessor;
    public OrganizationScope(T client, HttpClientOrganizationAccessor accessor)
    {
        _accessor = accessor;
        Client = client;
    }
    
    public T Client { get; }
    
    public void Dispose()
    {
        _accessor.Organization = null;
    }
}