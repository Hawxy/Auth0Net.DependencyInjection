#if NET8_0_OR_GREATER
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Auth0Net.DependencyInjection;

/// <summary>
/// Extensions used to enhance Auth0 client resilience.
/// </summary>
public static class Auth0ResilienceExtensions
{
    /// <summary>
    /// Adds enhanced rate limiting support to the Auth0 Client.
    /// </summary>
    /// <param name="builder">The underlying <see cref="IHttpClientBuilder"/></param>
    /// <param name="maxRetryAttempts">The max number of retry attempts to Auth0. Defaults to 10.</param>
    /// <returns></returns>
    public static IHttpResiliencePipelineBuilder AddAuth0RateLimitResilience(this IHttpClientBuilder builder, int maxRetryAttempts = 10)
    {
        return builder.AddResilienceHandler("RateLimitRetry",
            pipelineBuilder =>
            {
                pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
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
                    MaxRetryAttempts = maxRetryAttempts,
                    Delay = TimeSpan.FromSeconds(2)
                });
            });
    }
}

#endif