using System;
using Auth0.AuthenticationApi;
using Auth0.ManagementApi;
using Auth0Net.DependencyInjection.Organizations;
using FakeItEasy;
using Xunit;

namespace Auth0Net.DependencyInjection.Tests;

public class OrganizationScopeFactoryTests
{
    [Fact]
    public void OrganizationScopeFactory_ThrowsForAuthenticationApiClient()
    {
        var client = A.Fake<IAuthenticationApiClient>();
        var accessor = new HttpClientOrganizationAccessor();

#pragma warning disable AUTH0_EXPERIMENTAL
        Assert.Throws<InvalidOperationException>(() =>
            new OrganizationScopeFactory<IAuthenticationApiClient>(client, accessor));
#pragma warning restore AUTH0_EXPERIMENTAL
    }

    [Fact]
    public void OrganizationScopeFactory_ThrowsForManagementApiClient()
    {
        var client = A.Fake<IManagementApiClient>();
        var accessor = new HttpClientOrganizationAccessor();

#pragma warning disable AUTH0_EXPERIMENTAL
        Assert.Throws<InvalidOperationException>(() =>
            new OrganizationScopeFactory<IManagementApiClient>(client, accessor));
#pragma warning restore AUTH0_EXPERIMENTAL
    }

    [Fact]
    public void OrganizationScopeFactory_CreateScope_SetsOrganizationOnAccessorAndExposesClient()
    {
        var client = new TestClient();
        var accessor = new HttpClientOrganizationAccessor();

#pragma warning disable AUTH0_EXPERIMENTAL
        var factory = new OrganizationScopeFactory<TestClient>(client, accessor);
        var scope = factory.CreateScope("org-123");
#pragma warning restore AUTH0_EXPERIMENTAL

        Assert.Equal("org-123", accessor.Organization);
        Assert.Same(client, scope.Client);
        
        scope.Dispose();
        Assert.Null(accessor.Organization);
    }
    
    private sealed class TestClient { }
}
