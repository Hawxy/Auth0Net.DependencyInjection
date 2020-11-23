using Auth0Net.DependencyInjection.HttpClient;
using Xunit;

namespace Auth0Net.DependencyInjection.Tests
{
    public class UriHelperTests
    {
        [Theory]
        [InlineData("hawxy.au.auth0.com")]
        [InlineData("hawxy.au.auth0.com/")]
        [InlineData("http://hawxy.au.auth0.com/")]
        [InlineData("https://hawxy.au.auth0.com/")]
        public void GetValidManagementUri_Returns_ValidManagementUri(string domain)
        {
           Assert.Equal("https://hawxy.au.auth0.com/api/v2/", UriHelpers.GetValidManagementUri(domain).ToString()); 
        }

        [Theory]
        [InlineData("hawxy.au.auth0.com")]
        [InlineData("hawxy.au.auth0.com/")]
        [InlineData("http://hawxy.au.auth0.com/")]
        [InlineData("https://hawxy.au.auth0.com/")]
        public void GetValidUri_Returns_ValidUri(string domain)
        {
            Assert.Equal("https://hawxy.au.auth0.com/", UriHelpers.GetValidUri(domain).ToString());
        }
    }
}
