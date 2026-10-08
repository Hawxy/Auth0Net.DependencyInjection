using Auth0.Core.Exceptions;

namespace Auth0Net.DependencyInjection.Cache;

/// <summary>
/// Thrown when an On-Behalf-Of token exchange fails.
/// </summary>
/// <remarks>
/// A 401 means the subject token is missing, invalid or expired. A 403 means the client, scope or organization is not allowed.
/// A 429 means Auth0 rate limited the request; see <see cref="RetryAfter"/>.
/// </remarks>
public sealed class Auth0OnBehalfOfException : Exception
{
    /// <summary>
    /// Creates a new <see cref="Auth0OnBehalfOfException"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="error">The OAuth error code, such as <c>invalid_grant</c>.</param>
    /// <param name="errorDescription">The error description.</param>
    /// <param name="retryAfter">How long to wait before retrying, when rate limited.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public Auth0OnBehalfOfException(int statusCode, string? error, string? errorDescription, TimeSpan? retryAfter = null, Exception? innerException = null)
        : base(BuildMessage(statusCode, error, errorDescription), innerException)
    {
        StatusCode = statusCode;
        Error = error;
        ErrorDescription = errorDescription;
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// The HTTP status code of the failed exchange.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// The OAuth error code, such as <c>invalid_grant</c> or <c>access_denied</c>.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// The error description returned by Auth0.
    /// </summary>
    public string? ErrorDescription { get; }

    /// <summary>
    /// How long to wait before retrying. Only set for rate-limited (429) requests, when Auth0 provides it.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    internal static Auth0OnBehalfOfException SubjectTokenExpired() =>
        new(401, "invalid_grant", "The subject token has expired.");

    internal static Auth0OnBehalfOfException SubjectTokenMissing() =>
        new(401, "invalid_request", "No subject token was provided for the request.");

    internal static Auth0OnBehalfOfException From(ErrorApiException ex) =>
        new((int)ex.StatusCode, ex.ApiError?.Error, ex.ApiError?.Message ?? ex.Message, null, ex);

    internal static Auth0OnBehalfOfException From(RateLimitApiException ex) =>
        new(429, ex.ApiError?.Error ?? "too_many_requests", ex.ApiError?.Message ?? ex.Message, GetRetryAfter(ex.RateLimit), ex);

    private static TimeSpan? GetRetryAfter(RateLimit? rateLimit)
    {
        if (rateLimit is null)
            return null;

        if (rateLimit.RetryAfter > 0)
            return TimeSpan.FromSeconds(rateLimit.RetryAfter);

        if (rateLimit.Reset is { } reset)
        {
            var remaining = reset - DateTimeOffset.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        return null;
    }

    private static string BuildMessage(int statusCode, string? error, string? errorDescription)
    {
        var message = $"Auth0 On-Behalf-Of token exchange failed with status code {statusCode}";
        if (!string.IsNullOrEmpty(error))
            message += $" ({error})";
        if (!string.IsNullOrEmpty(errorDescription))
            message += $": {errorDescription}";
        return message;
    }
}
