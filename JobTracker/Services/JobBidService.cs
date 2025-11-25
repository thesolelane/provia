using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobTracker.Services
{
    /// <summary>
    /// Service for managing job bids
    /// Sub-contractors bid on jobs, foreman accepts with payment terms
    /// </summary>
    public interface IJobBidService
    {
        Task<JobBid?> PlaceBidAsync(int jobId, int subContractorId, decimal bidAmount, string? description);
        Task<JobBid?> AcceptBidAsync(int bidId, string paymentMethod, decimal advancePercentage, int acceptedByUserId);
        Task<JobBid?> RejectBidAsync(int bidId, int rejectedByUserId);
        Task<bool> ApproveWorkAsync(int bidId, int supervisorUserId, bool inspectionPassed, string? notes);
        Task<List<JobBid>> GetBidsForJobAsync(int jobId);
        Task<List<JobBid>> GetBidsForSubContractorAsync(int subContractorId);
        Task<List<JobBid>> GetPendingBidsForForemanAsync(int companyId);
        Task<JobBid?> GetBidAsync(int bidId);
    }

    public class JobBidService : IJobBidService
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<JobBidService> _logger;

        public JobBidService(JobTrackerContext context, ILogger<JobBidService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Sub-contractor places a bid on a job
        /// </summary>
        public async Task<JobBid?> PlaceBidAsync(int jobId, int subContractorId, decimal bidAmount, string? description)
        {
            try
            {
                var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == jobId);
                if (job == null)
                {
                    _logger.LogWarning($"Job {jobId} not found");
                    return null;
                }

                var sub = await _context.Users.FirstOrDefaultAsync(u => u.Id == subContractorId && u.Role == 2010);
                if (sub == null)
                {
                    _logger.LogWarning($"Sub-contractor {subContractorId} not found");
                    return null;
                }

                // Check for duplicate bid
                var existingBid = await _context.JobBids
                    .FirstOrDefaultAsync(b => b.JobId == jobId && b.SubContractorUserId == subContractorId && b.Status != "WITHDRAWN");

                if (existingBid != null)
                {
                    _logger.LogWarning($"Sub already bid on this job");
                    return null;
                }

                var bid = new JobBid
                {
                    JobId = jobId,
                    SubContractorUserId = subContractorId,
                    CompanyId = job.CompanyId ?? 0,
                    BidAmount = bidAmount,
                    BidDescription = description,
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                };

                _context.JobBids.Add(bid);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Sub {subContractorId} placed bid on job {jobId}");
                return bid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing bid");
                return null;
            }
        }

        /// <summary>
        /// Foreman accepts a bid (sub selects payment method, gets advance %)
        /// </summary>
        public async Task<JobBid?> AcceptBidAsync(int bidId, string paymentMethod, decimal advancePercentage, int acceptedByUserId)
        {
            try
            {
                var bid = await _context.JobBids
                    .Include(b => b.Job)
                    .FirstOrDefaultAsync(b => b.Id == bidId);

                if (bid == null || bid.Status != "PENDING")
                {
                    _logger.LogWarning("Bid not found or not pending");
                    return null;
                }

                bid.Status = "ACCEPTED";
                bid.AcceptedAt = DateTime.UtcNow;
                bid.AcceptedByUserId = acceptedByUserId;
                bid.PaymentMethod = paymentMethod;
                bid.AdvancePercentage = advancePercentage;
                bid.AdvanceAmount = bid.BidAmount * (advancePercentage / 100m);
                bid.RemainingAmount = bid.BidAmount - bid.AdvanceAmount;
                bid.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Bid {bidId} accepted by user {acceptedByUserId} with {advancePercentage}% advance");
                return bid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting bid");
                return null;
            }
        }

        /// <summary>
        /// Foreman rejects a bid
        /// </summary>
        public async Task<JobBid?> RejectBidAsync(int bidId, int rejectedByUserId)
        {
            try
            {
                var bid = await _context.JobBids.FirstOrDefaultAsync(b => b.Id == bidId);

                if (bid == null || bid.Status != "PENDING")
                {
                    _logger.LogWarning("Bid not found or not pending");
                    return null;
                }

                bid.Status = "REJECTED";
                bid.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Bid {bidId} rejected by user {rejectedByUserId}");
                return bid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting bid");
                return null;
            }
        }

        /// <summary>
        /// Supervisor approves completed work (after inspection if needed)
        /// </summary>
        public async Task<bool> ApproveWorkAsync(int bidId, int supervisorUserId, bool inspectionPassed, string? notes)
        {
            try
            {
                var bid = await _context.JobBids.FirstOrDefaultAsync(b => b.Id == bidId && b.Status == "ACCEPTED");

                if (bid == null)
                {
                    _logger.LogWarning("Bid not found or not accepted");
                    return false;
                }

                bid.SupervisorApproved = true;
                bid.SupervisorApprovedAt = DateTime.UtcNow;
                bid.SupervisorApprovedByUserId = supervisorUserId;
                bid.SupervisorNotes = notes;
                bid.IsInspectionPassed = inspectionPassed;
                bid.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Bid {bidId} approved by supervisor {supervisorUserId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving work");
                return false;
            }
        }

        /// <summary>
        /// Get all bids for a specific job
        /// </summary>
        public async Task<List<JobBid>> GetBidsForJobAsync(int jobId)
        {
            try
            {
                return await _context.JobBids
                    .Where(b => b.JobId == jobId)
                    .Include(b => b.SubContractor)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bids for job");
                return new List<JobBid>();
            }
        }

        /// <summary>
        /// Get all bids placed by a sub-contractor
        /// </summary>
        public async Task<List<JobBid>> GetBidsForSubContractorAsync(int subContractorId)
        {
            try
            {
                return await _context.JobBids
                    .Where(b => b.SubContractorUserId == subContractorId)
                    .Include(b => b.Job)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bids for sub-contractor");
                return new List<JobBid>();
            }
        }

        /// <summary>
        /// Get pending bids for foreman to review
        /// </summary>
        public async Task<List<JobBid>> GetPendingBidsForForemanAsync(int companyId)
        {
            try
            {
                return await _context.JobBids
                    .Where(b => b.CompanyId == companyId && b.Status == "PENDING")
                    .Include(b => b.Job)
                    .Include(b => b.SubContractor)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending bids");
                return new List<JobBid>();
            }
        }

        /// <summary>
        /// Get specific bid by ID
        /// </summary>
        public async Task<JobBid?> GetBidAsync(int bidId)
        {
            try
            {
                return await _context.JobBids
                    .Include(b => b.Job)
                    .Include(b => b.SubContractor)
                    .FirstOrDefaultAsync(b => b.Id == bidId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bid");
                return null;
            }
        }
    }
}
