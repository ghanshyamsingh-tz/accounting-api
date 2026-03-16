using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Accounting.Identity.Infrastructure.Resilience;

/// <summary>
/// Polly resilience policies for handling transient failures.
/// Provides retry, circuit breaker, and timeout policies for external service calls.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Retry policy for transient HTTP failures.
    /// Retries 3 times with exponential backoff (2, 4, 8 seconds).
    /// </summary>
    public static AsyncRetryPolicy HttpRetryPolicy => Policy
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (exception, timeSpan, retryCount, context) =>
            {
                // Log retry attempt
                Console.WriteLine($"Retry {retryCount} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
            });

    /// <summary>
    /// Circuit breaker policy for external services.
    /// Opens circuit after 5 consecutive failures, stays open for 30 seconds.
    /// </summary>
    public static AsyncCircuitBreakerPolicy HttpCircuitBreakerPolicy => Policy
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .CircuitBreakerAsync(
            exceptionsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (exception, duration) =>
            {
                Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s due to: {exception.Message}");
            },
            onReset: () =>
            {
                Console.WriteLine("Circuit breaker reset");
            });

    /// <summary>
    /// Timeout policy for external service calls.
    /// Times out after 10 seconds.
    /// </summary>
    public static AsyncTimeoutPolicy HttpTimeoutPolicy => Policy
        .TimeoutAsync(
            timeout: TimeSpan.FromSeconds(10),
            timeoutStrategy: TimeoutStrategy.Pessimistic);

    /// <summary>
    /// Combined policy: timeout → retry → circuit breaker.
    /// Apply in this order for proper fault handling.
    /// </summary>
    public static IAsyncPolicy HttpResiliencePolicy => Policy.WrapAsync(
        HttpCircuitBreakerPolicy,
        HttpRetryPolicy,
        HttpTimeoutPolicy);

    /// <summary>
    /// Retry policy for database operations.
    /// Retries 2 times with exponential backoff (1, 2 seconds).
    /// </summary>
    public static AsyncRetryPolicy DatabaseRetryPolicy => Policy
        .Handle<Exception>(ex => 
            ex.Message.Contains("timeout") || 
            ex.Message.Contains("deadlock") ||
            ex.Message.Contains("connection"))
        .WaitAndRetryAsync(
            retryCount: 2,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(retryAttempt),
            onRetry: (exception, timeSpan, retryCount, context) =>
            {
                Console.WriteLine($"Database retry {retryCount} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
            });

    /// <summary>
    /// Retry policy for message broker operations (Kafka).
    /// Retries 3 times with fixed 2-second delay.
    /// </summary>
    public static AsyncRetryPolicy MessageBrokerRetryPolicy => Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: _ => TimeSpan.FromSeconds(2),
            onRetry: (exception, timeSpan, retryCount, context) =>
            {
                Console.WriteLine($"Message broker retry {retryCount} after {timeSpan.TotalSeconds}s due to: {exception.Message}");
            });
}
