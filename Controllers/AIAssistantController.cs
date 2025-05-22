using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JobTrackerApp.Models;
using JobTrackerApp.Services.AI;
using JobTrackerApp.Data;

namespace JobTrackerApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AIAssistantController : ControllerBase
    {
        private readonly AIAssistantService _aiService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AIAssistantController> _logger;

        public AIAssistantController(
            AIAssistantService aiService,
            ApplicationDbContext context,
            ILogger<AIAssistantController> logger)
        {
            _aiService = aiService;
            _context = context;
            _logger = logger;
        }

        // POST: api/AIAssistant/building-code
        [HttpPost("building-code")]
        public async Task<ActionResult<AIResponse>> GetBuildingCodeAssistance(BuildingCodeQuery query)
        {
            try
            {
                string response = await _aiService.GetBuildingCodeAssistance(query.Query, query.SectionType);
                
                _logger.LogInformation("Generated AI response for building code query: {Query}", query.Query);
                
                return Ok(new AIResponse
                {
                    Response = response,
                    Query = query.Query,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI building code assistance");
                return StatusCode(500, "An error occurred while getting AI assistance");
            }
        }

        // POST: api/AIAssistant/section-guidance/{sectionId}
        [HttpPost("section-guidance/{sectionId}")]
        public async Task<ActionResult<AIResponse>> GetSectionGuidance(int sectionId)
        {
            try
            {
                var section = await _context.JobSections
                    .FindAsync(sectionId);
                
                if (section == null)
                {
                    return NotFound("Section not found");
                }

                string guidance = await _aiService.GetJobSectionGuidance(section);
                
                _logger.LogInformation("Generated AI guidance for job section {SectionType} with ID {SectionId}", 
                    section.SectionType, sectionId);
                
                return Ok(new AIResponse
                {
                    Response = guidance,
                    Query = $"Guidance for {section.SectionType.GetDisplayName()} section",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI section guidance for section {SectionId}", sectionId);
                return StatusCode(500, "An error occurred while getting AI guidance");
            }
        }

        // POST: api/AIAssistant/general-question
        [HttpPost("general-question")]
        public async Task<ActionResult<AIResponse>> GetGeneralAssistance(GeneralQuery query)
        {
            try
            {
                // Append construction/building context to ensure relevant answers
                string enhancedQuery = $"As a construction professional in Massachusetts working on a {query.JobType} project, I need information about: {query.Query}";
                
                string response = await _aiService.GetBuildingCodeAssistance(enhancedQuery, null);
                
                _logger.LogInformation("Generated AI response for general query: {Query}", query.Query);
                
                return Ok(new AIResponse
                {
                    Response = response,
                    Query = query.Query,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI general assistance");
                return StatusCode(500, "An error occurred while getting AI assistance");
            }
        }
    }

    public class BuildingCodeQuery
    {
        public string Query { get; set; } = string.Empty;
        public SectionType? SectionType { get; set; }
    }

    public class GeneralQuery
    {
        public string Query { get; set; } = string.Empty;
        public string JobType { get; set; } = "renovation";
    }

    public class AIResponse
    {
        public string Response { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
