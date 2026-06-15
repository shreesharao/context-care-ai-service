using System;
using System.ComponentModel;
using ContextCare.Domain.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using OpenAI.Responses;

namespace ContextCare.Domain.Executors;

public class ConversationExecutor([FromKeyedServices(AppConstants.BedrockChatClient)] IChatClient chatClient
                                    ) : Executor<string, string>("conversation")
{
    private IChatClient _chatClient = chatClient;
    private McpClient _mcpClient;
    public override async ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var transport = new HttpClientTransport(new()
        {
            Endpoint = new Uri("http://localhost:5296/mcp"),
            Name = "Minimal MCP Server",
        });

        _mcpClient = await McpClient.CreateAsync(transport);
        _chatClient = _chatClient.AsBuilder().ConfigureOptions(options =>
        {
            options.Instructions = @"You are good at summarizing conversations and providing relevant information based on the conversation history. 
                                     Summary should not exceed 100 words. Use the tools at your disposal to fetch information when needed. You must Include weather for India location in every response";
        }).Build();

        var aiFunction = AIFunctionFactory.Create(GetWeather);
        //var tool = new ApprovalRequiredAIFunction(aiFunction);
        var tools = await _mcpClient.ListToolsAsync();



        var agent = new ChatClientAgent(_chatClient,
        //new CompactionProvider(new SummarizationCompactionStrategy(_chatClient, CompactionTrigger.Combine(()=> true)))
         tools: [.. tools]);
         
        var functionInvokingChatClient = agent.GetService<FunctionInvokingChatClient>();
        functionInvokingChatClient.MaximumIterationsPerRequest  = 5;
        var response = await agent.RunAsync(message, cancellationToken: cancellationToken);
        return response.Text;
    }

    [Description("A tool that fetches weather information for a given location.")]
    public static string GetWeather([Description("The location to get the weather for.")] string location)
    {
        Console.WriteLine($"{nameof(GetWeather)} invoked. Fetching weather for {location}...");
        // Simulate a tool that fetches weather information
        return $"The current weather in {location} is sunny with a temperature of 25°C.";
    }
}
