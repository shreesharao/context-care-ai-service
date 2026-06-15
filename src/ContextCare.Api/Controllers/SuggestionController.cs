using ContextCare.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContextCare.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuggestionController : ControllerBase
    {
        private readonly IContextCareOrchService _contextCareOrchService;
        private readonly IKnowledgeService _knowledgeService;
        public SuggestionController(IContextCareOrchService contextCareOrchService, IKnowledgeService knowledgeService)
        {
            _contextCareOrchService = contextCareOrchService;
            _knowledgeService = knowledgeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSuggestions()
        {
            try
            {
                return Ok(await _contextCareOrchService.GetSuggestionsAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while processing the request: {ex.Message}");
            }
        }
        [HttpGet("rag")]
        public async Task<IActionResult> SearchKnowledgeBaseAsync(string query)
        {
            return Ok(await _knowledgeService.SearchKnowledgeBaseAsync(query));
        }
    }
}
