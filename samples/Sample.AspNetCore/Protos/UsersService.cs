using System.Threading.Tasks;
using Auth0.ManagementApi;
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
            var user = await _client.Users.GetAsync(request.UserId);

            return new UserResponse
            {
                User = new UserDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email
                }
            };
        }
    }
}
