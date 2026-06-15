using ContextCare.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ContextCare.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly ILogger<FilesController> _logger;
        private readonly IKnowledgeService _knowledgeService;
        public FilesController(ILogger<FilesController> logger, IKnowledgeService knowledgeService)
        {
            _logger = logger;
            _knowledgeService = knowledgeService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // For demonstration, we will just read the file content and log it.
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0; // Reset stream position to the beginning
                await _knowledgeService.BuildKnowledgeBaseAsync(stream);
            }

            return Ok("File uploaded successfully.");
        }
    }
}
