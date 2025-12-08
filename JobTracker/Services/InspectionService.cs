using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JobTracker.Data;
using JobTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JobTracker.Services
{
    /// <summary>
    /// Service for managing multi-stage inspections and permit documents
    /// Stages: ROUGH → SECOND → FINISH → FINAL_SIGNOFF
    /// </summary>
    public interface IInspectionService
    {
        Task<InspectionStage?> CreateInspectionAsync(int jobBidId, int companyId, string stage, string? checklistItems, string? requiredPermits);
        Task<InspectionStage?> SubmitInspectionAsync(int inspectionId, int inspectorId, bool passed, string? notes);
        Task<bool> UploadPermitAsync(int inspectionId, int jobBidId, int companyId, string permitType, string? permitNumber, string? documentUrl, string? fileName, int uploadedByUserId);
        Task<bool> ApprovePermitAsync(int permitId, int reviewedByUserId, bool approved, string? notes);
        Task<List<InspectionStage>> GetInspectionsForBidAsync(int jobBidId);
        Task<List<UploadedPermitDoc>> GetPermitsForInspectionAsync(int inspectionId);
        Task<bool> CanAdvanceToNextStageAsync(int jobBidId);
        Task<string?> GetNextInspectionStageAsync(int jobBidId);
    }

    public class InspectionService : IInspectionService
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<InspectionService> _logger;

        private static readonly List<string> InspectionStages = new() { "ROUGH", "SECOND", "FINISH", "FINAL_SIGNOFF" };

        public InspectionService(JobTrackerContext context, ILogger<InspectionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Create inspection for a job bid at specific stage
        /// </summary>
        public async Task<InspectionStage?> CreateInspectionAsync(int jobBidId, int companyId, string stage, string? checklistItems, string? requiredPermits)
        {
            try
            {
                var bid = await _context.JobBids.FirstOrDefaultAsync(b => b.Id == jobBidId);
                if (bid == null)
                {
                    _logger.LogWarning("JobBid not found");
                    return null;
                }

                var inspection = new InspectionStage
                {
                    JobBidId = jobBidId,
                    CompanyId = companyId,
                    Stage = stage,
                    Status = "PENDING",
                    ChecklistItems = checklistItems,
                    RequiredPermits = requiredPermits,
                    CreatedAt = DateTime.UtcNow
                };

                _context.InspectionStages.Add(inspection);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Inspection created for bid {jobBidId} at stage {stage}");
                return inspection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating inspection");
                return null;
            }
        }

        /// <summary>
        /// Inspector submits inspection results
        /// </summary>
        public async Task<InspectionStage?> SubmitInspectionAsync(int inspectionId, int inspectorId, bool passed, string? notes)
        {
            try
            {
                var inspection = await _context.InspectionStages.FirstOrDefaultAsync(i => i.Id == inspectionId);
                if (inspection == null)
                {
                    _logger.LogWarning("Inspection not found");
                    return null;
                }

                inspection.InspectorUserId = inspectorId;
                inspection.Passed = passed;
                inspection.InspectionNotes = notes;
                inspection.Status = passed ? "PASSED" : "FAILED";
                inspection.InspectionDate = DateTime.UtcNow;
                inspection.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Inspection {inspectionId} submitted: {(passed ? "PASSED" : "FAILED")}");
                return inspection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting inspection");
                return null;
            }
        }

        /// <summary>
        /// Sub-contractor uploads signed permit document
        /// </summary>
        public async Task<bool> UploadPermitAsync(int inspectionId, int jobBidId, int companyId, string permitType, string? permitNumber, string? documentUrl, string? fileName, int uploadedByUserId)
        {
            try
            {
                var inspection = await _context.InspectionStages.FirstOrDefaultAsync(i => i.Id == inspectionId);
                if (inspection == null)
                {
                    _logger.LogWarning("Inspection not found");
                    return false;
                }

                var permit = new UploadedPermitDoc
                {
                    InspectionStageId = inspectionId,
                    JobBidId = jobBidId,
                    CompanyId = companyId,
                    PermitType = permitType,
                    PermitNumber = permitNumber,
                    DocumentUrl = documentUrl,
                    FileName = fileName,
                    FileType = Path.GetExtension(fileName)?.TrimStart('.'),
                    Status = "PENDING_REVIEW",
                    UploadedByUserId = uploadedByUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UploadedPermitDocs.Add(permit);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Permit uploaded: {permitType} for inspection {inspectionId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading permit");
                return false;
            }
        }

        /// <summary>
        /// Admin/Foreman approves or rejects permit document
        /// </summary>
        public async Task<bool> ApprovePermitAsync(int permitId, int reviewedByUserId, bool approved, string? notes)
        {
            try
            {
                var permit = await _context.UploadedPermitDocs.FirstOrDefaultAsync(p => p.Id == permitId);
                if (permit == null)
                {
                    _logger.LogWarning("Permit not found");
                    return false;
                }

                permit.Status = approved ? "APPROVED" : "REJECTED";
                permit.ReviewedByUserId = reviewedByUserId;
                permit.ReviewNotes = notes;
                permit.ReviewedAt = DateTime.UtcNow;
                permit.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Permit {permitId} {(approved ? "APPROVED" : "REJECTED")} by user {reviewedByUserId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reviewing permit");
                return false;
            }
        }

        /// <summary>
        /// Get all inspections for a job bid
        /// </summary>
        public async Task<List<InspectionStage>> GetInspectionsForBidAsync(int jobBidId)
        {
            try
            {
                return await _context.InspectionStages
                    .Where(i => i.JobBidId == jobBidId)
                    .OrderBy(i => InspectionStages.IndexOf(i.Stage))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inspections");
                return new List<InspectionStage>();
            }
        }

        /// <summary>
        /// Get all permit documents for an inspection
        /// </summary>
        public async Task<List<UploadedPermitDoc>> GetPermitsForInspectionAsync(int inspectionId)
        {
            try
            {
                return await _context.UploadedPermitDocs
                    .Where(p => p.InspectionStageId == inspectionId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permits");
                return new List<UploadedPermitDoc>();
            }
        }

        /// <summary>
        /// Check if all required permits are approved for current stage
        /// </summary>
        public async Task<bool> CanAdvanceToNextStageAsync(int jobBidId)
        {
            try
            {
                var inspections = await GetInspectionsForBidAsync(jobBidId);
                var currentInspection = inspections.FirstOrDefault(i => i.Status == "PENDING" || i.Status == "PASSED");

                if (currentInspection == null)
                    return false;

                // All permits for current stage must be approved
                var permits = await GetPermitsForInspectionAsync(currentInspection.Id);
                var allApproved = permits.All(p => p.Status == "APPROVED");

                return allApproved && currentInspection.Passed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking advancement");
                return false;
            }
        }

        /// <summary>
        /// Get next inspection stage after current
        /// </summary>
        public async Task<string?> GetNextInspectionStageAsync(int jobBidId)
        {
            try
            {
                var inspections = await GetInspectionsForBidAsync(jobBidId);
                var lastCompletedIndex = inspections
                    .Where(i => i.Status != "PENDING")
                    .Max(i => InspectionStages.IndexOf(i.Stage));

                var nextIndex = lastCompletedIndex + 1;
                return nextIndex < InspectionStages.Count ? InspectionStages[nextIndex] : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next stage");
                return null;
            }
        }
    }
}
