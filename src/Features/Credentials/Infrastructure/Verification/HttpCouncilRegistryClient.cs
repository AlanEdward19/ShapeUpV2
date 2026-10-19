namespace ShapeUp.Features.Credentials.Infrastructure.Verification;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Verification;

/// <summary>
/// Calls the configured lookup service (an official API/agreement or a verification vendor). It never scrapes
/// the councils' pages. Timeout, retries and the circuit breaker keep a council outage from affecting submissions:
/// every failure becomes <see cref="CouncilLookupStatus.Unavailable"/>.
/// </summary>
public class HttpCouncilRegistryClient(
    HttpClient httpClient,
    IOptions<CouncilVerificationOptions> options,
    CouncilCircuitBreaker breaker,
    ILogger<HttpCouncilRegistryClient>? logger = null) : ICouncilRegistryClient
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(250);

    private sealed record Payload(
        [property: JsonPropertyName("found")] bool Found,
        [property: JsonPropertyName("active")] bool Active,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("region")] string? Region);

    public async Task<CouncilLookupResult> LookupAsync(string authority, string region, string number, CancellationToken cancellationToken)
    {
        var config = options.Value.For(authority);
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.BaseUrl))
            return CouncilLookupResult.Unavailable();
        if (breaker.IsOpen(authority))
        {
            logger?.LogWarning("{Authority} lookup skipped: circuit open", authority);
            return CouncilLookupResult.Unavailable();
        }

        var separator = config.BaseUrl.Contains('?') ? "&" : "?";
        var url = $"{config.BaseUrl.Trim()}{separator}number={Uri.EscapeDataString(number)}&region={Uri.EscapeDataString(region)}";

        for (var attempt = 0; attempt <= Math.Max(0, config.Retries); attempt++)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, config.TimeoutSeconds)));
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrWhiteSpace(config.ApiKey))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

                using var response = await httpClient.SendAsync(request, timeout.Token);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    breaker.RecordSuccess(authority);
                    return new CouncilLookupResult(CouncilLookupStatus.NotFound);
                }

                if (response.IsSuccessStatusCode)
                {
                    var payload = await response.Content.ReadFromJsonAsync<Payload>(timeout.Token);
                    breaker.RecordSuccess(authority);
                    if (payload is null)
                        return CouncilLookupResult.Unavailable();
                    return payload.Found
                        ? new CouncilLookupResult(CouncilLookupStatus.Found, payload.Active, payload.Name, payload.Region)
                        : new CouncilLookupResult(CouncilLookupStatus.NotFound);
                }

                logger?.LogWarning("{Authority} lookup answered {Status}", authority, (int)response.StatusCode);
                // 4xx other than 404 (bad key, bad request) will not improve on retry.
                if ((int)response.StatusCode < 500)
                    break;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException)
            {
                logger?.LogWarning(ex, "{Authority} lookup failed (attempt {Attempt})", authority, attempt + 1);
            }

            if (attempt < config.Retries)
                await Task.Delay(RetryDelay, cancellationToken);
        }

        breaker.RecordFailure(authority, config.BreakerThreshold, TimeSpan.FromMinutes(Math.Max(1, config.BreakerMinutes)));
        return CouncilLookupResult.Unavailable();
    }
}
