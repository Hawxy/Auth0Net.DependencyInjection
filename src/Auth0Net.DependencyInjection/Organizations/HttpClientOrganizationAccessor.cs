namespace Auth0Net.DependencyInjection.Organizations;

/// <summary>
/// Provides a mechanism for setting and retrieving the current organization context
/// using <see cref="AsyncLocal{T}"/>.
/// </summary>
public sealed class HttpClientOrganizationAccessor
{
    private readonly AsyncLocal<string?> _organization = new();

    /// <summary>
    /// Gets or sets the current organization context within the <see cref="HttpClientOrganizationAccessor"/>.
    /// </summary>
    public string? Organization
    {
        get => _organization.Value;
        set => _organization.Value = value;
    }
}