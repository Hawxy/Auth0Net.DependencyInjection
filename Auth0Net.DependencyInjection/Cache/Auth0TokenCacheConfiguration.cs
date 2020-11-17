using System;
using System.Collections.Generic;
using System.Text;

namespace Auth0NET.DependencyInjection.Cache
{
    public class Auth0TokenCacheConfiguration
    {
        public string Domain { get; set; } = null!;
        public string ClientId { get; set; } = null!;
        public string ClientSecret { get; set; } = null!;
    }
}
