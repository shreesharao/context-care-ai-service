using ContextCare.Domain.Interfaces;
using ContextCare.Domain.Models;
using Microsoft.Agents.AI.Workflows;

namespace ContextCare.Domain.Executors;

public class RetrieveExecutor(IKnowledgeService knowledgeService) : Executor<string, SuggestionContext>("RetrieveExecutor")
{
    private readonly IKnowledgeService _knowledgeService = knowledgeService;
    public override async ValueTask<SuggestionContext> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        var results = await _knowledgeService.SearchKnowledgeBaseAsync(message);
        return new SuggestionContext
        {
            Context = string.Join(Environment.NewLine, results),
            Question = message
        };
    }
}
