using System;
using System.Collections.Generic;
using System.Text;

namespace Auth0Net.DependencyInjection.Cache
{
    public class Auth0Configuration
    {
        public string Domain { get; set; } = null!;
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
    }
}
