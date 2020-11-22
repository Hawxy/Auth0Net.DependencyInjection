using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Sample.ConsoleApp.Services
{
    public class UsersService
    {
        private readonly HttpClient _client;

        public UsersService(HttpClient client)
        {
            _client = client;
        }

        public async Task<User[]?> GetUsersAsync() => await _client.GetFromJsonAsync<User[]>("users");

        public record User(string Id, string Name, string Email);
    }
}
