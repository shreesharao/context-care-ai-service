using System.Text.Json;
using ContextCare.Domain.Interfaces;
using ContextCare.Domain.Models;
using Pgvector.EntityFrameworkCore;
using SemanticSlicer;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace ContextCare.Domain.Services;

public class KnowledgeService : IKnowledgeService
{
    private readonly IPdfService _pdfService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IRepositoryService<KnowledgeBase> _repositoryService;
    public KnowledgeService(IPdfService pdfService,
                            IEmbeddingService embeddingService,
                            IRepositoryService<KnowledgeBase> repositoryService)
    {
        _pdfService = pdfService;
        _embeddingService = embeddingService;
        _repositoryService = repositoryService;
    }
    public async Task<bool> BuildKnowledgeBaseAsync(Stream fileStream)
    {
        var content = await _pdfService.ExtractTextFromPdfAsync(fileStream);
        await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, "extractedContent.txt"), content);
        ISlicer slicer = new Slicer();
        var documentChunks = slicer.GetDocumentChunks(content);
        foreach (var documentChunk in documentChunks)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(documentChunk.Content);
            //_store.Add(new VectorEntry { Text = documentChunk.Content, Vector = embedding.ToArray() });
            var knowledgeBaseEntry = new KnowledgeBase
            {
                Id = Guid.NewGuid().ToString(),
                Content = documentChunk.Content,
                Embeddings = new Pgvector.Vector(embedding.ToArray()),
                Source = "tbc"
            };
            await _repositoryService.AddAsync(knowledgeBaseEntry);
        }
        return true;
    }
    public async Task<List<string>> SearchKnowledgeBaseAsync(string searchQuery)
    {
        var queryVector = await _embeddingService.GenerateEmbeddingAsync(searchQuery);
        var items = await _repositoryService.Query()
                         .OrderBy(x => x.Embeddings.CosineDistance(new Vector(queryVector)))
                         .Take(5)
                         .ToListAsync();
        return items.Select(x => x.Content).ToList();
    }
}