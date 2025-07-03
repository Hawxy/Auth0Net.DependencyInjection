using System.Diagnostics.CodeAnalysis;

namespace Auth0Net.DependencyInjection.HttpClient;
#if NET8_0_OR_GREATER
/// <summary>
/// 
/// </summary>
public static class HttpClientOptionsExtensions
{
    private static readonly HttpRequestOptionsKey<string> OrgKey = new("auth0_org");

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="organization"></param>
    public static void SetOrganization(this HttpRequestOptions options, string organization)
    {
        options.Set(OrgKey, organization);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static string? GetOrganization(this HttpRequestOptions options)
    {
        options.TryGetValue(OrgKey, out var organization);
        return organization;
    }
}
#endif