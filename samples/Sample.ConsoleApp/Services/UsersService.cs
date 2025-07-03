using System.Net.Http.Json;
using Auth0Net.DependencyInjection.HttpClient;

namespace Sample.ConsoleApp.Services;

public class UsersService
{
    private readonly HttpClient _client;

    public UsersService(HttpClient client)
    {
        _client = client;
    }

    public async Task<User[]?> GetUsersInOrgAsync(string orgId, CancellationToken ct)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, "users");
        message.Options.SetOrganization(orgId);
        var response = await _client.SendAsync(message, ct);
        return await response.Content.ReadFromJsonAsync<User[]>(cancellationToken: ct);
    }
    
    public async Task<User[]?> GetUsersAsync(CancellationToken ct) => await _client.GetFromJsonAsync<User[]>("users", cancellationToken: ct);

    public record User(string Id, string Name, string Email);
}