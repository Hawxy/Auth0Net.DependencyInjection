using Auth0.ManagementApi;
using Grpc.Core;
using User;

namespace Sample.AspNetCore.Protos;

public class UsersService : UserService.UserServiceBase
{
    private readonly IManagementApiClient _client;
    public UsersService(IManagementApiClient client)
    {
        _client = client;
    }

    public override async Task<UserResponse> GetUser(UserRequest request, ServerCallContext context)
    {
        var users = await _client.Users.ListAsync(new ListUsersRequestParameters() { PerPage = 10});

        return new UserResponse
        {
            Users =
            {
                users.CurrentPage.Select(x=> new UserDto {UserId = x.UserId, Email = x.Email, FullName = x.Name})
            }
        };
    }
}