using System;

namespace Auth0Net.DependencyInjection.HttpClient
{
    /// <summary>
    /// Helpers that ensure Auth0 URLs are structured correctly.
    /// </summary>
    public static class UriHelpers
    {
        internal static Uri GetValidManagementUri(string domain) => new UriBuilder(domain) { Scheme = Uri.UriSchemeHttps, Path = "api/v2/", Port = 443 }.Uri;
        internal static Uri GetValidUri(string domain) => new UriBuilder(domain) { Scheme = Uri.UriSchemeHttps, Path = "/", Port = 443 }.Uri;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static string ToAuth0Uri(this string domain) => GetValidUri(domain).ToString();

    }
}
