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
    public class JobBidController : ControllerBase
    {
        private readonly IJobBidService _jobBidService;
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<JobBidController> _logger;

        public JobBidController(
            IJobBidService jobBidService,
            JobTrackerContext context,
            ITenantContext tenantContext,
            ILogger<JobBidController> logger)
        {
            _jobBidService = jobBidService;
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        /// <summary>
        /// Sub-contractor places a bid on a job
        /// </summary>
        [HttpPost("place-bid")]
        public async Task<ActionResult<JobBidDto>> PlaceBid([FromBody] PlaceBidRequest request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                // Only sub-contractors can place bids
                if (user.Role != 2010)
                    return Forbid("Only sub-contractors can place bids");

                var bid = await _jobBidService.PlaceBidAsync(
                    request.JobId,
                    user.Id,
                    request.BidAmount,
                    request.BidDescription);

                if (bid == null)
                    return BadRequest("Failed to place bid. Job may not exist or you already bid on it.");

                _logger.LogInformation($"Sub {user.Id} placed bid on job {request.JobId}");

                return Ok(new JobBidDto
                {
                    Id = bid.Id,
                    JobId = bid.JobId,
                    SubContractorUserId = bid.SubContractorUserId,
                    SubContractorName = user.GetDisplayName(),
                    Status = bid.Status,
                    BidAmount = bid.BidAmount,
                    BidDescription = bid.BidDescription,
                    CreatedAt = bid.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing bid");
                return StatusCode(500, new { message = "Error placing bid" });
            }
        }

        /// <summary>
        /// Foreman accepts a bid (sub selects payment method, advance %)
        /// </summary>
        [HttpPost("accept-bid")]
        public async Task<ActionResult<JobBidDto>> AcceptBid([FromBody] AcceptBidRequest request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                // Only admin/foreman can accept bids
                if (user.Role != 1510 && user.Role != 1520)
                    return Forbid("Only admins and foremans can accept bids");

                var bid = await _jobBidService.AcceptBidAsync(
                    request.BidId,
                    request.PaymentMethod,
                    request.AdvancePercentage,
                    user.Id);

                if (bid == null)
                    return BadRequest("Failed to accept bid. Bid may not exist or not pending.");

                _logger.LogInformation($"Bid {request.BidId} accepted by {user.Id} with {request.AdvancePercentage}% advance");

                return Ok(new JobBidDto
                {
                    Id = bid.Id,
                    JobId = bid.JobId,
                    Status = bid.Status,
                    BidAmount = bid.BidAmount,
                    PaymentMethod = bid.PaymentMethod,
                    AdvancePercentage = bid.AdvancePercentage,
                    AdvanceAmount = bid.AdvanceAmount,
                    RemainingAmount = bid.RemainingAmount,
                    CreatedAt = bid.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting bid");
                return StatusCode(500, new { message = "Error accepting bid" });
            }
        }

        /// <summary>
        /// Foreman rejects a bid
        /// </summary>
        [HttpPost("reject-bid/{bidId}")]
        public async Task<ActionResult> RejectBid(int bidId)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                // Only admin/foreman can reject bids
                if (user.Role != 1510 && user.Role != 1520)
                    return Forbid("Only admins and foremans can reject bids");

                var bid = await _jobBidService.RejectBidAsync(bidId, user.Id);

                if (bid == null)
                    return BadRequest("Failed to reject bid");

                _logger.LogInformation($"Bid {bidId} rejected by {user.Id}");
                return Ok(new { message = "Bid rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting bid");
                return StatusCode(500, new { message = "Error rejecting bid" });
            }
        }

        /// <summary>
        /// Supervisor approves completed work
        /// </summary>
        [HttpPost("approve-work")]
        public async Task<ActionResult> ApproveWork([FromBody] ApproveWorkRequest request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                // Only supervisors can approve work
                if (user.Role != 1530)
                    return Forbid("Only supervisors can approve work completion");

                var success = await _jobBidService.ApproveWorkAsync(
                    request.BidId,
                    user.Id,
                    request.InspectionPassed,
                    request.ApprovalNotes);

                if (!success)
                    return BadRequest("Failed to approve work");

                _logger.LogInformation($"Work approved by supervisor {user.Id} for bid {request.BidId}");
                return Ok(new { message = "Work approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving work");
                return StatusCode(500, new { message = "Error approving work" });
            }
        }

        /// <summary>
        /// Get pending bids for a job (admin/foreman view)
        /// </summary>
        [HttpGet("job/{jobId}")]
        public async Task<ActionResult<List<JobBidDto>>> GetBidsForJob(int jobId)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                var bids = await _jobBidService.GetBidsForJobAsync(jobId);

                var dtos = bids.Select(b => new JobBidDto
                {
                    Id = b.Id,
                    JobId = b.JobId,
                    SubContractorUserId = b.SubContractorUserId,
                    SubContractorName = b.SubContractor?.GetDisplayName() ?? "Unknown",
                    Status = b.Status,
                    BidAmount = b.BidAmount,
                    BidDescription = b.BidDescription,
                    PaymentMethod = b.PaymentMethod,
                    AdvancePercentage = b.AdvancePercentage,
                    AdvanceAmount = b.AdvanceAmount,
                    RemainingAmount = b.RemainingAmount,
                    SupervisorApproved = b.SupervisorApproved,
                    IsInspectionPassed = b.IsInspectionPassed,
                    CreatedAt = b.CreatedAt
                }).ToList();

                return Ok(new { count = dtos.Count, bids = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bids for job");
                return StatusCode(500, new { message = "Error retrieving bids" });
            }
        }

        /// <summary>
        /// Get all bids placed by current sub-contractor
        /// </summary>
        [HttpGet("my-bids")]
        public async Task<ActionResult<List<JobBidDto>>> GetMyBids()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                if (user.Role != 2010)
                    return Forbid("Only sub-contractors can view their bids");

                var bids = await _jobBidService.GetBidsForSubContractorAsync(user.Id);

                var dtos = bids.Select(b => new JobBidDto
                {
                    Id = b.Id,
                    JobId = b.JobId,
                    JobName = b.Job?.Name ?? "Unknown",
                    Status = b.Status,
                    BidAmount = b.BidAmount,
                    BidDescription = b.BidDescription,
                    PaymentMethod = b.PaymentMethod,
                    AdvancePercentage = b.AdvancePercentage,
                    AdvanceAmount = b.AdvanceAmount,
                    RemainingAmount = b.RemainingAmount,
                    SupervisorApproved = b.SupervisorApproved,
                    CreatedAt = b.CreatedAt
                }).ToList();

                return Ok(new { count = dtos.Count, bids = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my bids");
                return StatusCode(500, new { message = "Error retrieving your bids" });
            }
        }
    }
}
