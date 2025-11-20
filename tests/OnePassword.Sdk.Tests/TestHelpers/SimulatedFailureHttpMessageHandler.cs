// Test Helpers: Simulated Failure HTTP Message Handler
// Feature: 002-httpclient-factory-polly

using System.Net;

namespace OnePassword.Sdk.Tests.TestHelpers;

/// <summary>
/// Custom HttpMessageHandler for simulating HTTP failures in tests.
/// </summary>
/// <remarks>
/// Supports configurable failure patterns for testing retry logic, circuit breakers,
/// and timeout behavior without requiring actual network calls.
/// </remarks>
public class SimulatedFailureHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>> _responses = new();
    private int _requestCount;

    /// <summary>
    /// Gets the number of requests processed by this handler.
    /// </summary>
    public int RequestCount => _requestCount;

    /// <summary>
    /// Configures the handler to return a specific HTTP status code.
    /// </summary>
    /// <param name="statusCode">The status code to return.</param>
    /// <returns>This handler instance for fluent chaining.</returns>
    public SimulatedFailureHttpMessageHandler RespondWith(HttpStatusCode statusCode)
    {
        _responses.Enqueue(_ => Task.FromResult(new HttpResponseMessage(statusCode)));
        return this;
    }

    /// <summary>
    /// Configures the handler to return a custom response.
    /// </summary>
    /// <param name="response">The response to return.</param>
    /// <returns>This handler instance for fluent chaining.</returns>
    public SimulatedFailureHttpMessageHandler RespondWith(HttpResponseMessage response)
    {
        _responses.Enqueue(_ => Task.FromResult(response));
        return this;
    }

    /// <summary>
    /// Configures the handler to throw a specific exception.
    /// </summary>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>This handler instance for fluent chaining.</returns>
    public SimulatedFailureHttpMessageHandler ThrowException(Exception exception)
    {
        _responses.Enqueue(_ => Task.FromException<HttpResponseMessage>(exception));
        return this;
    }

    /// <summary>
    /// Configures the handler to simulate a timeout by throwing TaskCanceledException.
    /// </summary>
    /// <returns>This handler instance for fluent chaining.</returns>
    public SimulatedFailureHttpMessageHandler SimulateTimeout()
    {
        _responses.Enqueue(_ => Task.FromException<HttpResponseMessage>(
            new TaskCanceledException("The request timed out.")));
        return this;
    }

    /// <summary>
    /// Configures the handler to delay the response by a specific duration.
    /// </summary>
    /// <param name="delay">The delay duration.</param>
    /// <param name="statusCode">The status code to return after the delay.</param>
    /// <returns>This handler instance for fluent chaining.</returns>
    public SimulatedFailureHttpMessageHandler RespondAfterDelay(TimeSpan delay, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responses.Enqueue(async _ =>
        {
            await Task.Delay(delay);
            return new HttpResponseMessage(statusCode);
        });
        return this;
    }

    /// <summary>
    /// Configures the handler to use a custom response function.
    /// </summary>
    /// <param name="responseFunc">Function that generates the response based on the request.</param>
    /// <returns>This handler instance for fluent chaining.</returns>
    public SimulatedFailureHttpMessageHandler RespondWith(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFunc)
    {
        _responses.Enqueue(responseFunc);
        return this;
    }

    /// <summary>
    /// Resets the handler to its initial state, clearing all configured responses.
    /// </summary>
    public void Reset()
    {
        _responses.Clear();
        _requestCount = 0;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _requestCount);

        if (_responses.Count > 0)
        {
            var responseFunc = _responses.Dequeue();
            return responseFunc(request);
        }

        // Default response if no specific response is configured
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
    }
}
