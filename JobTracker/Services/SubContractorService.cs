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
    /// Service for managing sub-contractors across multiple companies
    /// Handles multi-company associations while maintaining company isolation
    /// </summary>
    public interface ISubContractorService
    {
        Task<List<SubContractorCompany>> GetCompaniesForSubContractorAsync(int subContractorUserId);
        Task<List<SubContractorCompany>> GetSubContractorsForCompanyAsync(int companyId);
        Task<SubContractorCompany?> GetSubContractorCompanyAsync(int subContractorUserId, int companyId);
        Task<bool> AddSubContractorToCompanyAsync(int subContractorUserId, int companyId, AddSubContractorRequest request);
        Task<bool> RemoveSubContractorFromCompanyAsync(int subContractorUserId, int companyId);
        Task<bool> VerifySubContractorAsync(int subContractorUserId, int companyId, int verifiedByUserId);
        Task<List<Job>> GetAvailableBidsForSubContractorAsync(int subContractorUserId);
        Task<List<Job>> GetAcceptedJobsForSubContractorAsync(int subContractorUserId);
    }

    public class SubContractorService : ISubContractorService
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<SubContractorService> _logger;

        public SubContractorService(JobTrackerContext context, ILogger<SubContractorService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all companies this sub-contractor works for
        /// </summary>
        public async Task<List<SubContractorCompany>> GetCompaniesForSubContractorAsync(int subContractorUserId)
        {
            try
            {
                return await _context.SubContractorCompanies
                    .Where(sc => sc.SubContractorUserId == subContractorUserId && sc.Status == "ACTIVE")
                    .Include(sc => sc.Company)
                    .OrderByDescending(sc => sc.LastActivityAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting companies for sub-contractor");
                return new List<SubContractorCompany>();
            }
        }

        /// <summary>
        /// Get all sub-contractors working for a specific company
        /// </summary>
        public async Task<List<SubContractorCompany>> GetSubContractorsForCompanyAsync(int companyId)
        {
            try
            {
                return await _context.SubContractorCompanies
                    .Where(sc => sc.CompanyId == companyId && sc.Status == "ACTIVE")
                    .Include(sc => sc.SubContractorUser)
                    .OrderByDescending(sc => sc.LastActivityAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sub-contractors for company");
                return new List<SubContractorCompany>();
            }
        }

        /// <summary>
        /// Get specific sub-contractor company relationship
        /// </summary>
        public async Task<SubContractorCompany?> GetSubContractorCompanyAsync(int subContractorUserId, int companyId)
        {
            try
            {
                return await _context.SubContractorCompanies
                    .FirstOrDefaultAsync(sc => 
                        sc.SubContractorUserId == subContractorUserId && 
                        sc.CompanyId == companyId && 
                        sc.Status == "ACTIVE");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sub-contractor company relationship");
                return null;
            }
        }

        /// <summary>
        /// Add a sub-contractor to work for a company
        /// </summary>
        public async Task<bool> AddSubContractorToCompanyAsync(int subContractorUserId, int companyId, AddSubContractorRequest request)
        {
            try
            {
                // Check if already exists
                var existing = await _context.SubContractorCompanies
                    .FirstOrDefaultAsync(sc => sc.SubContractorUserId == subContractorUserId && sc.CompanyId == companyId);

                if (existing != null)
                {
                    _logger.LogWarning($"Sub-contractor already associated with company");
                    return false;
                }

                var subContractorCompany = new SubContractorCompany
                {
                    SubContractorUserId = subContractorUserId,
                    CompanyId = companyId,
                    Status = "ACTIVE",
                    Specializations = request.Specializations,
                    BillingRate = request.BillingRate,
                    PaymentTerms = request.PaymentTerms,
                    AddedAt = DateTime.UtcNow
                };

                _context.SubContractorCompanies.Add(subContractorCompany);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Sub-contractor {subContractorUserId} added to company {companyId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding sub-contractor to company");
                return false;
            }
        }

        /// <summary>
        /// Remove a sub-contractor from a company
        /// </summary>
        public async Task<bool> RemoveSubContractorFromCompanyAsync(int subContractorUserId, int companyId)
        {
            try
            {
                var relationship = await _context.SubContractorCompanies
                    .FirstOrDefaultAsync(sc => sc.SubContractorUserId == subContractorUserId && sc.CompanyId == companyId);

                if (relationship == null)
                {
                    _logger.LogWarning("Sub-contractor company relationship not found");
                    return false;
                }

                relationship.Status = "REMOVED";
                relationship.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Sub-contractor {subContractorUserId} removed from company {companyId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing sub-contractor from company");
                return false;
            }
        }

        /// <summary>
        /// Verify a sub-contractor for a company (admin approval)
        /// </summary>
        public async Task<bool> VerifySubContractorAsync(int subContractorUserId, int companyId, int verifiedByUserId)
        {
            try
            {
                var relationship = await _context.SubContractorCompanies
                    .FirstOrDefaultAsync(sc => sc.SubContractorUserId == subContractorUserId && sc.CompanyId == companyId);

                if (relationship == null)
                {
                    _logger.LogWarning("Sub-contractor company relationship not found");
                    return false;
                }

                relationship.IsVerified = true;
                relationship.VerifiedAt = DateTime.UtcNow;
                relationship.VerifiedByUserId = verifiedByUserId;
                relationship.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Sub-contractor {subContractorUserId} verified for company {companyId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying sub-contractor");
                return false;
            }
        }

        /// <summary>
        /// Get available job bids for a sub-contractor
        /// Shows jobs from ALL companies they work with
        /// </summary>
        public async Task<List<Job>> GetAvailableBidsForSubContractorAsync(int subContractorUserId)
        {
            try
            {
                // Get all companies this sub works for
                var companies = await _context.SubContractorCompanies
                    .Where(sc => sc.SubContractorUserId == subContractorUserId && sc.Status == "ACTIVE")
                    .Select(sc => sc.CompanyId)
                    .ToListAsync();

                if (!companies.Any())
                    return new List<Job>();

                // Get active/pending jobs from these companies
                return await _context.Jobs
                    .Where(j => j.CompanyId.HasValue && companies.Contains(j.CompanyId.Value) && (j.Status == "Planning" || j.Status == "InProgress"))
                    .OrderByDescending(j => j.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available bids");
                return new List<Job>();
            }
        }

        /// <summary>
        /// Get jobs a sub-contractor has accepted
        /// </summary>
        public async Task<List<Job>> GetAcceptedJobsForSubContractorAsync(int subContractorUserId)
        {
            try
            {
                // Get all companies this sub works for
                var companies = await _context.SubContractorCompanies
                    .Where(sc => sc.SubContractorUserId == subContractorUserId && sc.Status == "ACTIVE")
                    .Select(sc => sc.CompanyId)
                    .ToListAsync();

                if (!companies.Any())
                    return new List<Job>();

                // TODO: Query JobBid table once created to find accepted bids
                // For now, return jobs in progress/assigned to this company
                return await _context.Jobs
                    .Where(j => j.CompanyId.HasValue && companies.Contains(j.CompanyId.Value) && j.Status == "InProgress")
                    .OrderByDescending(j => j.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting accepted jobs");
                return new List<Job>();
            }
        }
    }
}
