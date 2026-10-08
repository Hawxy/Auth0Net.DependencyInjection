using Microsoft.IdentityModel.Tokens;
using ZiggyCreatures.Caching.Fusion;

namespace Auth0Net.DependencyInjection.Cache;

/// <summary>
/// Credentials of the Auth0 Custom API client used for On-Behalf-Of token exchange.
/// </summary>
/// <remarks>
/// <see cref="ClientId"/> is required, along with either <see cref="ClientSecret"/>, or <see cref="ClientAssertionSecurityKey"/> and <see cref="ClientAssertionSecurityKeyAlgorithm"/>.
/// The domain is taken from the <see cref="Auth0Configuration"/> of the registered authentication client.
/// </remarks>
public sealed class Auth0OnBehalfOfConfiguration
{
    /// <summary>
    /// The Client ID of the Auth0 Custom API client.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// The Client Secret of the Auth0 Custom API client.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// The security key used to sign a client assertion (Private Key JWT), as an alternative to <see cref="ClientSecret"/>.
    /// </summary>
    public SecurityKey? ClientAssertionSecurityKey { get; set; }

    /// <summary>
    /// The signing algorithm for <see cref="ClientAssertionSecurityKey"/>, such as <see cref="SecurityAlgorithms.RsaSha256"/>.
    /// </summary>
    public string? ClientAssertionSecurityKeyAlgorithm { get; set; }

    /// <summary>
    /// The FusionCache instance used for exchanged tokens.
    /// Defaults to <see cref="Auth0Configuration.FusionCacheResolver"/> if set, otherwise the instance used by this library.
    /// </summary>
    public Func<IFusionCacheProvider, IFusionCache>? FusionCacheResolver { get; set; }

    /// <summary>
    /// Whether exchanged tokens are read from and written to the distributed level of the FusionCache instance, if it has one. Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// Exchanged tokens are user access tokens, so only enable this if the distributed cache is appropriately secured.
    /// </remarks>
    public bool UseDistributedCache { get; set; }

    internal bool IsValid() =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        (!string.IsNullOrWhiteSpace(ClientSecret) ||
         (ClientAssertionSecurityKey is not null && !string.IsNullOrWhiteSpace(ClientAssertionSecurityKeyAlgorithm)));
}
