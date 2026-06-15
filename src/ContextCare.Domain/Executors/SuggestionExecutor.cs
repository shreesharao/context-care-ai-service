using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using ContextCare.Domain.Models;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContextCare.Domain.Executors;

public class SuggestionExecutor([FromKeyedServices(AppConstants.BedrockChatClient)] IChatClient chatClient, ILogger<SuggestionExecutor> logger) : Executor<SuggestionContext, string>("SuggestionExecutor")
{
    private readonly ILogger<SuggestionExecutor> _logger = logger;
    private readonly IChatClient _chatClient = chatClient;
    public override async ValueTask<string> HandleAsync(SuggestionContext suggestionContext, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Role = ChatRole.System,
                Contents = [new TextContent("You are good at answering questions based on the provided context. Use the context to give an accurate and relevant answer.")],
                AdditionalProperties = new AdditionalPropertiesDictionary()
                {
                    { nameof(ContentBlock.CachePoint), new CachePointBlock(){Type = CachePointType.Default} }
                }
            },
            new ChatMessage
            {
                Role = ChatRole.User,
                Contents = [new TextContent($"Context: {suggestionContext.Context}\n\nQuestion: {suggestionContext.Question}")]
            }
        };
        var options = new ChatOptions()
        {
            MaxOutputTokens = 4096,
        };
        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken, options: options);
        return response.Text;
    }

    protected override async ValueTask OnCheckpointingAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        await context.QueueStateUpdateAsync("SuggestionExecutorState", "Some state to checkpoint", cancellationToken);
    }

    protected override async ValueTask OnCheckpointRestoredAsync(IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var state = await context.ReadStateAsync<string>("SuggestionExecutorState", cancellationToken);
        Console.WriteLine($"Restored state in {nameof(SuggestionExecutor)}: {state}");
    }
}
