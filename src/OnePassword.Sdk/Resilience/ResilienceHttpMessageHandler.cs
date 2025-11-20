// Resilience: HTTP Message Handler with Polly v8 Pipeline
// Feature: 002-httpclient-factory-polly

using Polly;

namespace OnePassword.Sdk.Resilience;

/// <summary>
/// HTTP message handler that applies a Polly v8 resilience pipeline to outgoing requests.
/// </summary>
/// <remarks>
/// Wraps HTTP requests with retry, circuit breaker, and timeout policies configured via ResiliencePipeline.
/// </remarks>
public class ResilienceHttpMessageHandler : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    /// <summary>
    /// Initializes a new instance of the ResilienceHttpMessageHandler.
    /// </summary>
    /// <param name="pipeline">The configured resilience pipeline to apply.</param>
    /// <param name="innerHandler">The inner handler to delegate actual HTTP requests to. If null, IHttpClientFactory will set it.</param>
    public ResilienceHttpMessageHandler(
        ResiliencePipeline<HttpResponseMessage> pipeline,
        HttpMessageHandler? innerHandler = null)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));

        // Only set InnerHandler if provided (manual creation scenario)
        // For IHttpClientFactory, leave it null - factory will set it
        if (innerHandler != null)
        {
            InnerHandler = innerHandler;
        }
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Execute the request through the resilience pipeline
        return await _pipeline.ExecuteAsync(
            async ct => await base.SendAsync(request, ct),
            cancellationToken);
    }
}
