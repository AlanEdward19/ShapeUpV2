namespace ShapeUp.Configurations;

using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ShapeUp.Features.Authorization.Shared.Data;

/// <summary>
/// Observability foundation (ROADMAP.md Fase 1 -- Observabilidade day one).
///
/// Exports traces, metrics, and logs via OTLP/HTTP to a local, self-hosted Seq instance
/// (see docker-compose.yml's `seq` service) -- no SaaS account/API key needed to get the
/// day-one SLIs the roadmap calls for:
///   - availability      -> /health/live, /health/ready (below)
///   - p95/p99, error rate -> ASP.NET Core's built-in `http.server.request.duration` histogram,
///                            auto-collected by OpenTelemetry.Instrumentation.AspNetCore
///   - payment success   -> no billing feature exists yet (Fase 5); nothing to instrument
///
/// "sync success"/"queue delay"/"crash-free sessions" are frontend SLIs (mutation queue +
/// ErrorBoundary in ShapeUp-Web) -- out of scope here.
///
/// `Observability:OtlpEndpoint` in appsettings.json points at Seq's base URL. Swap it for a
/// real collector's endpoint later without touching this file.
/// </summary>
public static class ObservabilityExtensions
{
    private const string ServiceName = "ShapeUpApi";

    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var otlpEndpoint = configuration["Observability:OtlpEndpoint"] ?? "http://localhost:8081";

        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(ServiceName);

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(ServiceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri($"{otlpEndpoint}/ingest/otlp/v1/traces");
                    o.Protocol = OtlpExportProtocol.HttpProtobuf;
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri($"{otlpEndpoint}/ingest/otlp/v1/metrics");
                    o.Protocol = OtlpExportProtocol.HttpProtobuf;
                }));

        services.AddLogging(logging => logging.AddOpenTelemetry(o =>
        {
            o.SetResourceBuilder(resourceBuilder);
            o.IncludeFormattedMessage = true;
            o.IncludeScopes = true;
            o.AddOtlpExporter(exporter =>
            {
                exporter.Endpoint = new Uri($"{otlpEndpoint}/ingest/otlp/v1/logs");
                exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
        }));

        // Availability SLI: liveness (process is up) and readiness (DB reachable).
        services.AddHealthChecks()
            .AddDbContextCheck<AuthorizationDbContext>("sql-server", tags: ["ready"]);

        return services;
    }
}
