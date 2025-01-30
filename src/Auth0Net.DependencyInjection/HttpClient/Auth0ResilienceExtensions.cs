#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Auth0Net.DependencyInjection.HttpClient;

/// <summary>
/// Extensions used to enhance Auth0 client resilience.
/// </summary>
public static class Auth0ResilienceExtensions
{
    /// <summary>
    /// Adds enhanced rate limiting support to the Auth0 Client. This API is experimental.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    [Experimental("Auth0DIExperimental")]
    public static IHttpResiliencePipelineBuilder AddAuth0RateLimitResilience(this IHttpClientBuilder builder)
    {
        return builder.AddResilienceHandler("RateLimitRetry",
            static builder =>
            {
                // See: https://www.pollydocs.org/strategies/retry.html
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    // Disable the default handling of Retry-After header
                    ShouldRetryAfterHeader = false,
                    DelayGenerator = static args =>
                    {
                        if(args.Outcome.Result?.StatusCode is (HttpStatusCode)429
                           && args.Outcome.Result.Headers.TryGetValues("x-ratelimit-reset", out var headers)
                           && long.TryParse(headers.First(), out var ticks))
                        {
                            var retryAt = DateTimeOffset.FromUnixTimeSeconds(ticks);
                            var timeSpan = retryAt - DateTimeOffset.UtcNow;
                            return new ValueTask<TimeSpan?>(timeSpan);
                        }

                        return new ValueTask<TimeSpan?>((TimeSpan?)null);
                    },
    
                    MaxRetryAttempts = 10,
                    Delay = TimeSpan.FromSeconds(2)
                });
            });
    }
}

#endif