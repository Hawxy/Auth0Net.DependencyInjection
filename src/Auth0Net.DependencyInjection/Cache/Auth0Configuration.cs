using System;

namespace Auth0Net.DependencyInjection.Cache
{
    /// <summary>
    /// Configuration for the Auth0 Clients and Auth0 Token Cache.
    /// </summary>
    public class Auth0Configuration
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
        /// The time before access token expiry that the token should be renewed. The default is 30 minutes. 
        /// </summary>
        public TimeSpan TokenExpiryBuffer { get; set; } = TimeSpan.FromMinutes(30);
       
    }

    /// <summary>
    /// Configuration for the Auth0 Management API.
    /// </summary>
    public class Auth0ManagementClientConfiguration
    {
        /// <summary>
        /// This option will replace the use of <see cref="Auth0Configuration.Domain"/> to compute the audience for the Management API token.
        /// Useful when using custom domains.
        /// </summary>
        public string? AudienceDomainOverride { get; set; }
    }
}
