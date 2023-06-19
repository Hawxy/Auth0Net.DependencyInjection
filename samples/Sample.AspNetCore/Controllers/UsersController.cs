using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;

namespace Sample.AspNetCore.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IManagementApiClient _managementApiClient;

    public UsersController(IManagementApiClient managementApiClient)
    {
        _managementApiClient = managementApiClient;
    }

    [HttpGet]
    public async Task<ActionResult<User[]>> Get()
    {
        var user = await _managementApiClient.Users.GetAllAsync(new GetUsersRequest(), new PaginationInfo());

        return user.Select(x => new User(x.UserId, x.FullName, x.Email)).ToArray();
    }
}

public record User(string Id, string Name, string Email);