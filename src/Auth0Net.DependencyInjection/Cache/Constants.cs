namespace Auth0Net.DependencyInjection.Cache;

internal static class Constants
{
    public const string FusionCacheInstance = $"Cache:{nameof(Auth0TokenCache)}";
}

/// <summary>
/// Registered alongside the named FusionCache instance so it is only added once.
/// </summary>
internal sealed class Auth0FusionCacheMarker;