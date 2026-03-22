using ContextCare.Domain.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Scalar.AspNetCore;
using OpenTelemetry;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(AppConstants.ApplicationName))
    .WithLogging()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation())
    .UseOtlpExporter(OpenTelemetry.Exporter.OtlpExportProtocol.Grpc,
                     Uri.TryCreate(builder.Configuration.GetValue<string>("openTelemetry:endpoint"),
                                   UriKind.Absolute,
                                   out var uri) ? uri : throw new InvalidOperationException("Invalid OpenTelemetry endpoint")); // Automatically collect HTTP metrics

builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy())

    // The "ready" check is a more complex check that might involve checking database connectivity, external service availability, etc.
    .AddAsyncCheck("ready", async () => HealthCheckResult.Healthy());

builder.Host.UseSerilog((context, sp, config) => config.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTheme(ScalarTheme.Saturn);
    });
}

app.MapHealthChecks("/livez", new HealthCheckOptions()
{
    Predicate = option => option.Tags.Contains("live"),
    ResponseWriter = (context, report) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Health check result {@Report}", report);
        return context.Response.WriteAsync(report.Status.ToString());
    }
});

app.MapHealthChecks("/readyz", new HealthCheckOptions()
{
    Predicate = check => check.Name.Equals("ready")
});

app.UseSerilogRequestLogging();

await app.RunAsync();