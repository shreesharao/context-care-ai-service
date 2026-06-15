using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using ContextCare.Domain.Interfaces;
using ContextCare.Domain.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace ContextCare.Domain.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IChatClient _chatClient;
    public EmbeddingService([FromKeyedServices(AppConstants.BedrockEmbeddingClient)] IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
                            [FromKeyedServices(AppConstants.BedrockChatClient)] IChatClient chatClient)
    {
        _embeddingGenerator = embeddingGenerator;
        _chatClient = chatClient;
    }
    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string chunk)
    {
        // var options = new EmbeddingGenerationOptions
        // {
        //     RawRepresentationFactory = generator =>
        //     {
        //         var body = new
        //         {
        //             texts = new[] { chunk },
        //             input_type = "search_document"
        //         };
        //         return new InvokeModelRequest
        //         {
        //             ModelId = "cohere.embed-english-v3",
        //             ContentType = "application/json",
        //             Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body)))
        //         };
        //     }
        // };

        var client = new AmazonBedrockRuntimeClient(region: RegionEndpoint.APSoutheast1);
        var body = new
        {
            texts = new[] { chunk },
            input_type = "search_document"
        };

        var request = new InvokeModelRequest
        {
            ModelId = "cohere.embed-english-v3",
            ContentType = "application/json",
            Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body)))
        };
        var response = await client.InvokeModelAsync(request);
        var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(response.Body);
        // await File.WriteAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "embeddingResponse.json"), response.Body.ToArray());
        //var embeddings = await _embeddingGenerator.GenerateAsync(chunk, options: options);

        return new ReadOnlyMemory<float>(embeddingResponse.Embeddings?[0] ?? Array.Empty<float>());
    }

}

internal sealed class EmbeddingResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    [JsonPropertyName("embeddings")]
    public List<float[]>? Embeddings { get; set; }

    [JsonPropertyName("texts")]
    public List<string>? Texts { get; set; }
}
