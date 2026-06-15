namespace ContextCare.Domain.Interfaces;

public interface IContextCareOrchService
{
    public Task<string> GetSuggestionsAsync();
}
