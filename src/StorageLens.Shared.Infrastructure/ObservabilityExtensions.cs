using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Json;
using Microsoft.AspNetCore.Diagnostics;
using FluentValidation;
using FluentValidation.AspNetCore;
using StorageLens.Shared.Infrastructure.Validators;

namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// Centralised observability configuration for all StorageLens services.
///
/// Registers:
///   - Serilog structured logging (JSON to stdout; enriched with ServiceName,
///     Environment, and the per-request CorrelationId pushed by CorrelationIdMiddleware)
///   - OpenTelemetry distributed tracing with ASP.NET Core + HttpClient instrumentation
///     * OTLP exporter when Observability:OtlpEndpoint is set (production / Docker)
///     * Console exporter fallback in Development
///   - IHttpContextAccessor + CorrelationIdDelegatingHandler (used by upstream HTTP clients)
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Call once per service in Program.cs, before builder.Build().
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder for the current service.</param>
    /// <param name="serviceName">Human-readable service name used in traces and log enrichment.</param>
    public static WebApplicationBuilder AddStorageLensObservability(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        // ── Structured logging via Serilog ───────────────────────────────────
        builder.Host.UseSerilog((context, services, cfg) =>
        {
            cfg
                .ReadFrom.Configuration(context.Configuration)   // honour appsettings overrides
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()                          // picks up CorrelationId pushed by middleware
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(new JsonFormatter());            // structured JSON for log aggregation
        });

        // ── OpenTelemetry distributed tracing ────────────────────────────────
        var otlpEndpoint = builder.Configuration["Observability:OtlpEndpoint"];

        builder.Services
            .AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(
                        ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.RecordException = true;
                        // Suppress noisy /health spans
                        opts.Filter = ctx =>
                            !ctx.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation(opts =>
                    {
                        opts.RecordException = true;
                    });

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    // Production / Docker: send to OTLP collector (Jaeger, Tempo, etc.)
                    tracing.AddOtlpExporter(o =>
                        o.Endpoint = new Uri(otlpEndpoint));
                }
                else if (builder.Environment.IsDevelopment())
                {
                    // Local dotnet run: print spans to console
                    tracing.AddConsoleExporter();
                }
            });

        // ── Supporting services ──────────────────────────────────────────────
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<CorrelationIdDelegatingHandler>();

        // ── Audit logging ────────────────────────────────────────────────────
        builder.Services.AddAuditLogger();

        // ── Input validation via FluentValidation ────────────────────────────
        // Registers all validators defined in Shared.Infrastructure.Validators.
        // Services that want per-assembly validators should also call
        //   builder.Services.AddValidatorsFromAssemblyContaining<Program>()
        // in their own Program.cs.
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddValidatorsFromAssemblyContaining<CreateStorageLocationRequestValidator>();

        // ── Global exception handling ────────────────────────────────────────
        // Registers RFC 7807 ProblemDetails + centralised IExceptionHandler so every
        // unhandled exception returns a consistent JSON error shape with a correlationId.
        // Services must also call app.UseExceptionHandler() in their pipeline.
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        return builder;
    }
}
