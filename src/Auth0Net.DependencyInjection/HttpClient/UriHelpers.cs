namespace Auth0Net.DependencyInjection.HttpClient;

/// <summary>
/// Helpers that ensure Auth0 URLs are structured correctly.
/// </summary>
public static class UriHelpers
{
    internal static Uri GetValidManagementUri(string domain) => new UriBuilder(domain) { Scheme = Uri.UriSchemeHttps, Path = "api/v2/", Port = 443 }.Uri;
    internal static Uri GetValidUri(string domain) => new UriBuilder(domain) { Scheme = Uri.UriSchemeHttps, Path = "/", Port = 443 }.Uri;
    /// <summary>
    /// Converts a naked domain into a https URL.
    /// </summary>
    /// <remarks>
    /// <c>my-auth0-tenant.au.auth0.com</c> --> <c>https://my-auth0-tenant.au.auth0.com/</c>
    /// </remarks>
    /// <param name="domain">The domain to convert.</param>
    /// <returns>The URL in string form.</returns>
    public static string ToHttpsUrl(this string domain) => GetValidUri(domain).ToString();
}