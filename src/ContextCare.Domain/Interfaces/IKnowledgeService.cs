using System;

namespace ContextCare.Domain.Interfaces;

public interface IKnowledgeService
{
    Task<bool> BuildKnowledgeBaseAsync(Stream fileStream);
    Task<List<string>> SearchKnowledgeBaseAsync(string searchQuery);
}
