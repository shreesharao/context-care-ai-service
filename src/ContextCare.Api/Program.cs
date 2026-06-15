using ContextCare.Domain.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Scalar.AspNetCore;
using OpenTelemetry;
using Serilog;
using Microsoft.Extensions.AI;
using OpenAI;
using ContextCare.Domain.Interfaces;
using ContextCare.Domain.Services;
using ContextCare.Domain.Executors;
using Amazon.BedrockRuntime;
using Amazon;
using ModelContextProtocol.Server;
using ContextCare.Domain.Utils;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(AppConstants.ApplicationName, serviceVersion: "1.0.0"))
    .WithLogging()
    .WithTracing(builder =>
    {
        builder.AddConsoleExporter();
        builder.AddSource(AppConstants.ApplicationName);
        builder.AddSource("Microsoft.AI.AgentFramework");
        builder.AddHttpClientInstrumentation(
        // Note: Only called on .NET & .NET Core runtimes.
        (options) =>
        {
            options.EnrichWithHttpRequestMessage =
              (activity, httpRequestMessage) =>
              {
                  activity.SetTag("request.content", httpRequestMessage.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty);
              };
            options.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
            {
                activity.SetTag("response.content", httpResponseMessage.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty);
            };
        });
        builder.AddAspNetCoreInstrumentation(o =>
        {
            o.EnrichWithHttpRequest = (activity, httpRequest) =>
            {
                activity.SetTag("requestProtocol", httpRequest.Protocol);
            };
            o.EnrichWithHttpResponse = (activity, httpResponse) =>
            {
                activity.SetTag("response", httpResponse.ContentLength);
            };
            o.EnrichWithException = (activity, exception) =>
            {
                if (exception.Source != null)
                {
                    activity.SetTag("exception.source", exception.Source);
                }
            };
        });
    })
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation())
    .UseOtlpExporter(OpenTelemetry.Exporter.OtlpExportProtocol.Grpc,
                     Uri.TryCreate(builder.Configuration.GetSection("openTelemetry").GetValue<string>("endpoint"),
                                   UriKind.Absolute,
                                   out var uri) ? uri : throw new InvalidOperationException("Invalid OpenTelemetry endpoint")); // Automatically collect HTTP metrics
var otlp = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy())

    // The "ready" check is a more complex check that might involve checking database connectivity, external service availability, etc.
    .AddAsyncCheck("ready", async () => HealthCheckResult.Healthy());

builder.Host.UseSerilog((context, sp, config) => config.ReadFrom.Configuration(context.Configuration));

//register IChatClient
builder.Services.AddKeyedSingleton<IChatClient>(AppConstants.OpenAIChatClient, (sp, key) =>
{
    var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new KeyNotFoundException();
    var openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? throw new KeyNotFoundException();
    var openAiClient = new OpenAIClient(openAiApiKey);
    return openAiClient.GetChatClient(openAiModel).AsIChatClient();
});

builder.Services.AddKeyedSingleton(AppConstants.BedrockChatClient, (sp, key) =>
{
    IAmazonBedrockRuntime runtime = new AmazonBedrockRuntimeClient(region: RegionEndpoint.APSoutheast1);
    return runtime.AsIChatClient("arn:aws:bedrock:ap-southeast-1:653108204233:inference-profile/global.amazon.nova-2-lite-v1:0")
                  .AsBuilder()
                  .UseChatReducer()
                  .UseOpenTelemetry(sourceName: AppConstants.ApplicationName, configure: opt => opt.EnableSensitiveData = true)
                  .Build();
});

builder.Services.AddKeyedSingleton(AppConstants.BedrockEmbeddingClient, (sp, key) =>
{
    IAmazonBedrockRuntime runtime = new AmazonBedrockRuntimeClient(region: RegionEndpoint.APSoutheast1);
    return runtime.AsIEmbeddingGenerator("cohere.embed-english-v3");
});

builder.Services.AddScoped<IContextCareOrchService, ContextCareOrchService>();
builder.Services.AddScoped<RetrieveExecutor>();
builder.Services.AddSingleton<SuggestionExecutor>();
builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
builder.Services.AddSingleton<IEmbeddingService, EmbeddingService>();
builder.Services.AddSingleton<IPdfService, PdfService>();
builder.Services.AddSingleton<ConversationExecutor>();
builder.Services.AddMcpServer().WithToolsFromAssembly(Assembly.GetAssembly(typeof(Tools))).WithHttpTransport(opt => opt.Stateless = true);
builder.Services.AddNpgsql<AppDbContext>(builder.Configuration.GetConnectionString("postgres"), opt => opt.UseVector());
builder.Services.AddScoped(typeof(IRepositoryService<>), typeof(RepositoryService<>));

builder.Services.AddControllers();

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

app.MapControllers();
app.MapMcp("/mcp");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();