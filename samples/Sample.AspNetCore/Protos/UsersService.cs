using System.Linq;
using System.Threading.Tasks;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;
using Grpc.Core;
using User;

namespace Sample.AspNetCore.Protos
{
    public class UsersService : UserService.UserServiceBase
    {
        private readonly ManagementApiClient _client;
        public UsersService(ManagementApiClient client)
        {
            _client = client;
        }

        public override async Task<UserResponse> GetUser(UserRequest request, ServerCallContext context)
        {
            var users = await _client.Users.GetAllAsync(new GetUsersRequest(), new PaginationInfo());

            return new UserResponse
            {
                Users =
                {
                    users.Select(x=> new UserDto {UserId = x.UserId, Email = x.Email, FullName = x.FullName})
                }
            };
        }
    }
}
