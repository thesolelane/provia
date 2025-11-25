using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTracker.Services;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InspectionController : ControllerBase
    {
        private readonly IInspectionService _inspectionService;
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<InspectionController> _logger;

        public InspectionController(
            IInspectionService inspectionService,
            JobTrackerContext context,
            ITenantContext tenantContext,
            ILogger<InspectionController> logger)
        {
            _inspectionService = inspectionService;
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        /// <summary>
        /// Create inspection for a job bid (admin/foreman)
        /// </summary>
        [HttpPost("create")]
        public async Task<ActionResult<InspectionStageDto>> CreateInspection([FromBody] dynamic request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                if (user.Role != 1510 && user.Role != 1520)
                    return Forbid("Only admins and foremans can create inspections");

                var companyId = _tenantContext.GetCurrentCompanyId();

                var inspection = await _inspectionService.CreateInspectionAsync(
                    (int)request.jobBidId,
                    companyId,
                    (string)request.stage,
                    (string?)request.checklistItems,
                    (string?)request.requiredPermits);

                if (inspection == null)
                    return BadRequest("Failed to create inspection");

                _logger.LogInformation($"Inspection created for bid {request.jobBidId}");

                return Ok(new InspectionStageDto
                {
                    Id = inspection.Id,
                    JobBidId = inspection.JobBidId,
                    Stage = inspection.Stage,
                    Status = inspection.Status,
                    ChecklistItems = inspection.ChecklistItems,
                    RequiredPermits = inspection.RequiredPermits
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating inspection");
                return StatusCode(500, new { message = "Error creating inspection" });
            }
        }

        /// <summary>
        /// Inspector submits inspection results
        /// </summary>
        [HttpPost("submit/{inspectionId}")]
        public async Task<ActionResult> SubmitInspection(int inspectionId, [FromBody] dynamic request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                // Inspectors (admin/foreman/supervisor can submit)
                if (user.Role != 1510 && user.Role != 1520 && user.Role != 1530)
                    return Forbid("Only authorized personnel can submit inspections");

                var inspection = await _inspectionService.SubmitInspectionAsync(
                    inspectionId,
                    user.Id,
                    (bool)request.passed,
                    (string?)request.notes);

                if (inspection == null)
                    return BadRequest("Failed to submit inspection");

                _logger.LogInformation($"Inspection {inspectionId} submitted by {user.Id}");
                return Ok(new { message = "Inspection submitted", passed = inspection.Passed });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting inspection");
                return StatusCode(500, new { message = "Error submitting inspection" });
            }
        }

        /// <summary>
        /// Sub-contractor uploads permit document
        /// </summary>
        [HttpPost("upload-permit")]
        public async Task<ActionResult> UploadPermit([FromBody] dynamic request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                var companyId = _tenantContext.GetCurrentCompanyId();

                var success = await _inspectionService.UploadPermitAsync(
                    (int)request.inspectionId,
                    (int)request.jobBidId,
                    companyId,
                    (string)request.permitType,
                    (string?)request.permitNumber,
                    (string?)request.documentUrl,
                    (string?)request.fileName,
                    user.Id);

                if (!success)
                    return BadRequest("Failed to upload permit");

                _logger.LogInformation($"Permit uploaded by {user.Id}");
                return Ok(new { message = "Permit uploaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading permit");
                return StatusCode(500, new { message = "Error uploading permit" });
            }
        }

        /// <summary>
        /// Admin/Foreman approves or rejects permit
        /// </summary>
        [HttpPost("approve-permit/{permitId}")]
        public async Task<ActionResult> ApprovePermit(int permitId, [FromBody] dynamic request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                if (user.Role != 1510 && user.Role != 1520)
                    return Forbid("Only admins and foremans can approve permits");

                var success = await _inspectionService.ApprovePermitAsync(
                    permitId,
                    user.Id,
                    (bool)request.approved,
                    (string?)request.notes);

                if (!success)
                    return BadRequest("Failed to approve permit");

                _logger.LogInformation($"Permit {permitId} reviewed by {user.Id}");
                return Ok(new { message = "Permit reviewed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing permit");
                return StatusCode(500, new { message = "Error reviewing permit" });
            }
        }

        /// <summary>
        /// Get all inspections for a job bid
        /// </summary>
        [HttpGet("bid/{jobBidId}")]
        public async Task<ActionResult<List<InspectionStageDto>>> GetInspectionsForBid(int jobBidId)
        {
            try
            {
                var inspections = await _inspectionService.GetInspectionsForBidAsync(jobBidId);

                var dtos = new List<InspectionStageDto>();
                foreach (var inspection in inspections)
                {
                    var permits = await _inspectionService.GetPermitsForInspectionAsync(inspection.Id);
                    dtos.Add(new InspectionStageDto
                    {
                        Id = inspection.Id,
                        JobBidId = inspection.JobBidId,
                        Stage = inspection.Stage,
                        Status = inspection.Status,
                        InspectionNotes = inspection.InspectionNotes,
                        Passed = inspection.Passed,
                        InspectionDate = inspection.InspectionDate,
                        ChecklistItems = inspection.ChecklistItems,
                        RequiredPermits = inspection.RequiredPermits,
                        PermitDocuments = permits.Select(p => new PermitDocumentDto
                        {
                            Id = p.Id,
                            PermitType = p.PermitType,
                            PermitNumber = p.PermitNumber,
                            Status = p.Status,
                            FileName = p.FileName,
                            IssuedDate = p.IssuedDate,
                            ExpiryDate = p.ExpiryDate,
                            ReviewNotes = p.ReviewNotes
                        }).ToList()
                    });
                }

                return Ok(new { count = dtos.Count, inspections = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inspections");
                return StatusCode(500, new { message = "Error retrieving inspections" });
            }
        }

        /// <summary>
        /// Check if bid can advance to next inspection stage
        /// </summary>
        [HttpGet("can-advance/{jobBidId}")]
        public async Task<ActionResult> CanAdvanceToNextStage(int jobBidId)
        {
            try
            {
                var canAdvance = await _inspectionService.CanAdvanceToNextStageAsync(jobBidId);
                var nextStage = await _inspectionService.GetNextInspectionStageAsync(jobBidId);

                return Ok(new
                {
                    canAdvance = canAdvance,
                    nextStage = nextStage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking advancement");
                return StatusCode(500, new { message = "Error checking advancement" });
            }
        }
    }
}
