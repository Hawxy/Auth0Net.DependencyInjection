namespace Auth0Net.DependencyInjection.Cache;

/// <summary>
/// Configuration for the Auth0 Clients and Auth0 Token Cache.
/// </summary>
public sealed class Auth0Configuration
{
    /// <summary>
    /// The default or custom root domain for your Auth0 tenant. 
    /// </summary>
    public string Domain { get; set; } = null!;
    /// <summary>
    /// The Client ID of the Auth0 Machine-to-Machine application.
    /// </summary>
    public string? ClientId { get; set; }
    /// <summary>
    /// The Client Secret of the Auth0 Machine-to-Machine application.
    /// </summary>
    public string? ClientSecret { get; set; }
}

/// <summary>
/// Configuration for the Auth0 Management API.
/// </summary>
public sealed class Auth0ManagementTokenConfiguration
{
    /// <summary>
    /// This option will replace the use of <see cref="Auth0Configuration.Domain"/> to compute the audience for the Management API token.
    /// Useful when using custom domains.
    /// </summary>
    public string? Audience { get; set; }
}