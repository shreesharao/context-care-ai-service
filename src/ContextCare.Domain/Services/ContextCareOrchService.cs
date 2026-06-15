using System.Data.Common;
using ContextCare.Domain.Executors;
using ContextCare.Domain.Interfaces;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;

namespace ContextCare.Domain.Services;

public class ContextCareOrchService : IContextCareOrchService
{
    private readonly RetrieveExecutor _retrieveExecutor;
    private readonly SuggestionExecutor _suggestionExecutor;
    private readonly ConversationExecutor _conversationExecutor;
    public ContextCareOrchService(RetrieveExecutor retrieveExecutor,
                                  SuggestionExecutor suggestionExecutor,
                                  ConversationExecutor conversationExecutor)
    {
        _retrieveExecutor = retrieveExecutor;
        _suggestionExecutor = suggestionExecutor;
        _conversationExecutor = conversationExecutor;
    }

    public async Task<string> GetSuggestionsAsync()
    {
        //This sub workflow can be added as an executor
        var sub = new WorkflowBuilder(_retrieveExecutor).Build().BindAsExecutor("sub");
        var builder = new WorkflowBuilder(_retrieveExecutor);
        
        builder.AddEdge(_retrieveExecutor, _suggestionExecutor)
               .AddEdge(_suggestionExecutor, _conversationExecutor)
               .WithOutputFrom(_conversationExecutor)
               .WithName("workflow1")
               .WithOpenTelemetry();

        var output = string.Empty;
        var workflow = builder.Build();
        
        var checkpointManager = CheckpointManager.CreateJson(new FileSystemJsonCheckpointStore(new DirectoryInfo(@"C:\D-Drive\workspace\git-ssr\context-care-ai-service\store")));
        var run = await InProcessExecution.RunAsync(workflow, "explain anonimization", checkpointManager: checkpointManager);
         
        foreach (var evt in run.OutgoingEvents)
        {
            switch (evt)
            {
                case WorkflowOutputEvent:
                    output = evt.Data as string;
                    break;
                default:
                    break;
            }
        }

        return output;
    }
}
