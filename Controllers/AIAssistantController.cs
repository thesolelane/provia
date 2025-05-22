using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using JobTracker.Services;
using JobTracker.Data;
using JobTracker.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AIAssistantController : ControllerBase
    {
        private readonly JobTrackerContext _context;
        private readonly AIService _aiService;
        private readonly ILogger<AIAssistantController> _logger;

        public AIAssistantController(
            JobTrackerContext context,
            AIService aiService,
            ILogger<AIAssistantController> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        // POST: api/AIAssistant/chat
        [HttpPost("chat")]
        public async Task<ActionResult<object>> GetChatResponse([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest("Message cannot be empty.");
                }

                // Get context for the chat based on job section if provided
                List<BuildingCode> relevantCodes = new List<BuildingCode>();
                JobSection section = null;

                if (request.JobSectionId.HasValue)
                {
                    section = await _context.JobSections
                        .Include(js => js.Job)
                        .FirstOrDefaultAsync(js => js.SectionId == request.JobSectionId);

                    if (section != null)
                    {
                        relevantCodes = await _context.BuildingCodes
                            .Where(bc => bc.RelatedSection == section.Type && bc.IsActive)
                            .ToListAsync();
                    }
                }

                // Get the AI response
                var response = await _aiService.GetAssistantResponse(
                    request.Message,
                    request.ConversationId,
                    section,
                    relevantCodes
                );

                return new
                {
                    Response = response,
                    ConversationId = request.ConversationId ?? Guid.NewGuid().ToString(),
                    Context = section != null
                        ? new
                        {
                            JobId = section.JobId,
                            JobName = section.Job?.JobName,
                            SectionId = section.SectionId,
                            SectionType = section.Type.ToString(),
                            RelevantCodes = relevantCodes.Take(3).Select(c => new
                            {
                                c.CodeNumber,
                                c.Title
                            })
                        }
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI assistant response for message: {Message}", request.Message);
                return StatusCode(500, "Internal server error occurred while processing your request.");
            }
        }

        // POST: api/AIAssistant/codequery
        [HttpPost("codequery")]
        public async Task<ActionResult<object>> GetCodeSpecificResponse([FromBody] CodeQueryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return BadRequest("Query cannot be empty.");
                }

                if (!request.SectionType.HasValue && string.IsNullOrWhiteSpace(request.Category))
                {
                    return BadRequest("Either section type or category must be provided.");
                }

                // Get relevant building codes based on section type or category
                var codesQuery = _context.BuildingCodes.Where(bc => bc.IsActive);

                if (request.SectionType.HasValue)
                {
                    codesQuery = codesQuery.Where(bc => bc.RelatedSection == request.SectionType.Value);
                }

                if (!string.IsNullOrWhiteSpace(request.Category))
                {
                    codesQuery = codesQuery.Where(bc => bc.Category == request.Category);
                }

                if (!string.IsNullOrWhiteSpace(request.Subcategory))
                {
                    codesQuery = codesQuery.Where(bc => bc.Subcategory == request.Subcategory);
                }

                var relevantCodes = await codesQuery.ToListAsync();

                // Get the AI response specific to building codes
                var response = await _aiService.GetCodeSpecificResponse(
                    request.Query,
                    relevantCodes
                );

                // Get the most relevant codes
                var mostRelevantCodes = await _aiService.GetMostRelevantCodes(
                    request.Query,
                    relevantCodes,
                    5
                );

                return new
                {
                    Response = response,
                    RelevantCodes = mostRelevantCodes.Select(c => new
                    {
                        c.CodeId,
                        c.CodeNumber,
                        c.Title,
                        c.Description,
                        c.Category,
                        c.Subcategory
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting code-specific AI response for query: {Query}", request.Query);
                return StatusCode(500, "Internal server error occurred while processing your code query.");
            }
        }

        public class ChatRequest
        {
            public string Message { get; set; }
            public string ConversationId { get; set; }
            public int? JobSectionId { get; set; }
        }

        public class CodeQueryRequest
        {
            public string Query { get; set; }
            public SectionType? SectionType { get; set; }
            public string Category { get; set; }
            public string Subcategory { get; set; }
        }
    }
}
