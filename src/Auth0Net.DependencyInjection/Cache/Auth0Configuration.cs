﻿namespace Auth0Net.DependencyInjection.Cache
{
    /// <summary>
    /// Configuration for the Auth0 Clients and Auth0 Token Cache.
    /// </summary>
    public class Auth0Configuration
    {
        /// <summary>
        /// The root domain for your Auth0 tenant.
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
}
