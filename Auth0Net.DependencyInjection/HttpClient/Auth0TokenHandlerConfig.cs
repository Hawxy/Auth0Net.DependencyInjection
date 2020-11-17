using System;
using System.Collections.Generic;
using System.Text;

namespace Auth0NET.DependencyInjection.HttpClient
{
    public class Auth0TokenHandlerConfig
    {
        public string Audience { get; set; } = null!;
    }
}
