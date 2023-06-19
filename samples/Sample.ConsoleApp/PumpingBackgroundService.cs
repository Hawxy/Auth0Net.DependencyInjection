using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.ConsoleApp.Services;
using User;

namespace Sample.ConsoleApp;

// Not a realistic example, just using it to hit our API.
public class PumpingBackgroundService : BackgroundService
{
    private readonly UsersService _usersService;
    private readonly UserService.UserServiceClient _usersClient;
    private readonly ILogger<PumpingBackgroundService> _logger;

    public PumpingBackgroundService(UsersService usersService, UserService.UserServiceClient usersClient, ILogger<PumpingBackgroundService> logger)
    {
        _usersService = usersService;
        _usersClient = usersClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var userHttpClient = await _usersService.GetUsersAsync();
            _logger.LogInformation("HttpClient got user's email: {email}", userHttpClient?.First().Email);
            await Task.Delay(5000, stoppingToken);

            var userGrpcClient = await _usersClient.GetUserAsync(new UserRequest(), cancellationToken: stoppingToken);
            _logger.LogInformation("HttpClient got user's email: {email}", userGrpcClient.Users.First().Email);
            await Task.Delay(5000, stoppingToken);
        }
    }
}