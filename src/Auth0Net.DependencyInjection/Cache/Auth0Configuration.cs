using ZiggyCreatures.Caching.Fusion;

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
    
    /// <summary>
    /// This package uses FusionCache internally for token caching.
    /// If you have an existing FusionCache configuration you'd like to use (ie one setup with a distributed cache), pass the name of it here.
    /// For the default FusionCache instance registered with ".AddFusionCache()", use <see cref="FusionCacheOptions.DefaultCacheName"/>
    /// </summary>
    public string? FusionCacheInstance { get; set; }
    
}

/// <summary>
/// Configuration for the Auth0 Management API.
/// </summary>
public sealed class Auth0ManagementClientConfiguration
{
    /// <summary>
    /// Sets the audience for the Management API token. This is your default Auth0 domain within your tenant and should be set if you're using a custom domain.
    /// </summary>
    public string? Audience { get; set; }
    
    /// <summary>
    /// Sets the max number of retries for the management client. Default is 2.
    /// </summary>
    public int? MaxRetries { get; set; }
    
    /// <summary>
    /// Sets the timeout for the management client. Default is 30 seconds.
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}