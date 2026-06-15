using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace ContextCare.Domain.Models;

public class KnowledgeBase
{
    public required string Id { get; set; }
    public required string Content { get; set; }
    [Column(TypeName = "vector(1024)")]
    public required Vector Embeddings { get; set; }
    public required string Source { get; set; }
}
