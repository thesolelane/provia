using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTracker.Services.AI;
using JobTracker.Services;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AIController : ControllerBase
    {
        private readonly AIAssistantService _ai;
        private readonly ILogger<AIController> _logger;
        private readonly ITenantContext _tenantContext;

        public AIController(AIAssistantService ai, ILogger<AIController> logger, ITenantContext tenantContext)
        {
            _ai = ai;
            _logger = logger;
            _tenantContext = tenantContext;
        }

        // POST: api/ai/generate-scope
        [HttpPost("generate-scope")]
        public async Task<ActionResult<object>> GenerateScope([FromBody] GenerateScopeRequest req)
        {
            try
            {
                _tenantContext.GetCurrentCompanyId();

                if (string.IsNullOrWhiteSpace(req.JobName))
                    return BadRequest(new { message = "Job name is required" });

                var result = await _ai.GenerateJobScopeAsync(
                    req.JobName,
                    req.Location ?? "",
                    req.ClientName ?? "",
                    req.Budget,
                    req.TradeHint);

                return Ok(new
                {
                    result.Description,
                    result.Notes,
                    AiPowered = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
                });
            }
            catch (Exception ex)
            {
                if (ex is TenantContextException)
                    return Unauthorized(new { message = "Authenticated company context is required" });

                _logger.LogError(ex, "Error generating job scope");
                return StatusCode(500, new { message = "Error generating scope" });
            }
        }

        // POST: api/ai/score-lead
        [HttpPost("score-lead")]
        public async Task<ActionResult<object>> ScoreLead([FromBody] ScoreLeadRequest req)
        {
            try
            {
                _tenantContext.GetCurrentCompanyId();

                var result = await _ai.ScoreLeadAsync(
                    req.CallerName ?? "Unknown",
                    req.Source,
                    req.Stage,
                    req.DaysInStage,
                    req.JobType,
                    req.JobScope,
                    req.HasEmail,
                    req.HasPhone);

                return Ok(new
                {
                    result.Score,
                    result.Tier,
                    result.Reason,
                    AiPowered = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
                });
            }
            catch (Exception ex)
            {
                if (ex is TenantContextException)
                    return Unauthorized(new { message = "Authenticated company context is required" });

                _logger.LogError(ex, "Error scoring lead");
                return StatusCode(500, new { message = "Error scoring lead" });
            }
        }

        // POST: api/ai/generate-invoice-items
        [HttpPost("generate-invoice-items")]
        public async Task<ActionResult<object>> GenerateInvoiceItems([FromBody] GenerateInvoiceRequest req)
        {
            try
            {
                _tenantContext.GetCurrentCompanyId();

                if (string.IsNullOrWhiteSpace(req.WorkDescription))
                    return BadRequest(new { message = "Work description is required" });

                var json = await _ai.GenerateInvoiceLineItemsAsync(req.WorkDescription, req.TotalBudget);
                return Ok(new
                {
                    LineItemsJson = json,
                    AiPowered = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
                });
            }
            catch (Exception ex)
            {
                if (ex is TenantContextException)
                    return Unauthorized(new { message = "Authenticated company context is required" });

                _logger.LogError(ex, "Error generating invoice items");
                return StatusCode(500, new { message = "Error generating invoice items" });
            }
        }

        // GET: api/ai/status
        [HttpGet("status")]
        public ActionResult<object> GetStatus()
        {
            try
            {
                _tenantContext.GetCurrentCompanyId();
            }
            catch (TenantContextException)
            {
                return Unauthorized(new { message = "Authenticated company context is required" });
            }

            var hasKey = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
            return Ok(new
            {
                AiEnabled  = hasKey,
                Model      = hasKey ? "gpt-4o" : "local-templates",
                Features   = new[] { "job-scope", "invoice-items", "fire-blocking" },
            });
        }
    }

    public class ScoreLeadRequest
    {
        public string? CallerName  { get; set; }
        public string? Source      { get; set; }
        public string? Stage       { get; set; }
        public int     DaysInStage { get; set; }
        public string? JobType     { get; set; }
        public string? JobScope    { get; set; }
        public bool    HasEmail    { get; set; }
        public bool    HasPhone    { get; set; }
    }

    public class GenerateScopeRequest
    {
        public string  JobName    { get; set; } = "";
        public string? Location   { get; set; }
        public string? ClientName { get; set; }
        public decimal Budget     { get; set; }
        public string? TradeHint  { get; set; }
    }

    public class GenerateInvoiceRequest
    {
        public string  WorkDescription { get; set; } = "";
        public decimal TotalBudget     { get; set; }
    }
}
