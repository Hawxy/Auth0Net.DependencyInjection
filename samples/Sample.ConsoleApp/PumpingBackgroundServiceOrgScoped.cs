using Auth0Net.DependencyInjection.Organizations;
using Sample.ConsoleApp.Services; 
#pragma warning disable AUTH0_EXPERIMENTAL

namespace Sample.ConsoleApp;

// Not a realistic example, just using it to hit our API.
public class PumpingBackgroundServiceOrgScoped : BackgroundService
{
    private readonly OrganizationScopeFactory<UsersService> _scopeFactory;
    private readonly ILogger<PumpingBackgroundService> _logger; 

    public PumpingBackgroundServiceOrgScoped(OrganizationScopeFactory<UsersService> scopeFactory, ILogger<PumpingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scopedClient = _scopeFactory.CreateScope("org_12345");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var userHttpClient = await scopedClient.Client.GetUsersAsync(stoppingToken);
            _logger.LogInformation("HttpClient got user's email: {email}", userHttpClient?.First().Email);
            await Task.Delay(5000, stoppingToken);
        }
    }
}