using System;
using System.Collections.Generic;
using System.Text;

namespace Auth0Net.DependencyInjection.HttpClient
{
    public static class Auth0Util
    {
        public static string GetAuth0ApiUrl(string domain)
        {
            var uriBuilder = new UriBuilder(new Uri(domain)) {Scheme = Uri.UriSchemeHttps, Path = "api/v2"};
            return uriBuilder.Uri.ToString();
        }
    }
}
