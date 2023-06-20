using System.Net.Http.Json;

namespace Sample.ConsoleApp.Services;

public class UsersService
{
    private readonly HttpClient _client;

    public UsersService(HttpClient client)
    {
        _client = client;
    }

    public async Task<User[]?> GetUsersAsync(CancellationToken ct) => await _client.GetFromJsonAsync<User[]>("users", cancellationToken: ct);

    public record User(string Id, string Name, string Email);
}