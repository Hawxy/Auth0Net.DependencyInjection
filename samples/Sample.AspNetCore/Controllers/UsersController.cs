using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Auth0.ManagementApi;

namespace Sample.AspNetCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ManagementApiClient _managementApiClient;

        public UsersController(ManagementApiClient managementApiClient)
        {
            _managementApiClient = managementApiClient;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> Get(string id)
        {
            var user = await _managementApiClient.Users.GetAsync(id);

            return new User(user.UserId, user.FullName, user.Email);
        }
    }

    public record User(string Id, string Name, string Email);
}
