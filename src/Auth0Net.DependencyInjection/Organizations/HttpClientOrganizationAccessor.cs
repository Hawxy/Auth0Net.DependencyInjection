namespace Auth0Net.DependencyInjection.Organizations;

public sealed class HttpClientOrganizationAccessor
{
    private readonly AsyncLocal<string?> _organization = new();

    public string? Organization
    {
        get => _organization.Value;
        set => _organization.Value = value;
    }
}