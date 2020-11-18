namespace Auth0NET.DependencyInjection.HttpClient
{
    public class Auth0TokenConfig
    {
        public string? Audience { get; set; }

        public Auth0TokenConfig() { }

        public Auth0TokenConfig(string audience)
        {
            Audience = audience;
        }
    }
}
