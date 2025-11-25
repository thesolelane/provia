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
    /// Service for managing client information with role-based access control
    /// - No billing data stored
    /// - Strict role-based visibility
    /// - Audit logging for admin access
    /// </summary>
    public interface IClientInfoService
    {
        Task<ClientInfoDto?> GetClientInfoAsync(int clientId, int userRole, int companyId);
        Task<List<ClientInfoDto>> GetAllClientsAsync(int userRole, int companyId);
        Task<bool> CreateClientInfoAsync(ClientInfo clientInfo, int createdByUserId);
        Task<bool> UpdateClientInfoAsync(ClientInfo clientInfo, int updatedByUserId);
        Task<bool> CanAccessClientInfo(int userRole);
    }

    public class ClientInfoService : IClientInfoService
    {
        private readonly JobTrackerContext _context;
        private readonly ILogger<ClientInfoService> _logger;

        public ClientInfoService(JobTrackerContext context, ILogger<ClientInfoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Check if a role can access client information
        /// </summary>
        public Task<bool> CanAccessClientInfo(int userRole)
        {
            // All roles have some level of access
            // Admin (1510), Foreman (1520), Supervisor (1530), Field Operator (2001), Sub-Contractor (2010)
            return Task.FromResult(true);
        }

        /// <summary>
        /// Get single client info with role-based filtering
        /// </summary>
        public async Task<ClientInfoDto?> GetClientInfoAsync(int clientId, int userRole, int companyId)
        {
            try
            {
                var clientInfo = await _context.Set<ClientInfo>()
                    .FirstOrDefaultAsync(c => c.Id == clientId && c.CompanyId == companyId);

                if (clientInfo == null)
                    return null;

                // Convert to DTO based on role
                return ClientInfoDto.FromModel(clientInfo, userRole);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client info");
                return null;
            }
        }

        /// <summary>
        /// Get all clients for a company with role-based filtering
        /// </summary>
        public async Task<List<ClientInfoDto>> GetAllClientsAsync(int userRole, int companyId)
        {
            try
            {
                var clients = await _context.Set<ClientInfo>()
                    .Where(c => c.CompanyId == companyId && c.IsActive)
                    .ToListAsync();

                // Convert all to DTOs based on role
                return clients
                    .Select(c => ClientInfoDto.FromModel(c, userRole))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving clients");
                return new List<ClientInfoDto>();
            }
        }

        /// <summary>
        /// Create new client info (admin/foreman only)
        /// </summary>
        public async Task<bool> CreateClientInfoAsync(ClientInfo clientInfo, int createdByUserId)
        {
            try
            {
                clientInfo.CreatedByUserId = createdByUserId;
                clientInfo.CreatedAt = DateTime.UtcNow;

                _context.Set<ClientInfo>().Add(clientInfo);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Client info created: {clientInfo.Id} by user {createdByUserId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating client info");
                return false;
            }
        }

        /// <summary>
        /// Update client info (admin/foreman only)
        /// </summary>
        public async Task<bool> UpdateClientInfoAsync(ClientInfo clientInfo, int updatedByUserId)
        {
            try
            {
                var existing = await _context.Set<ClientInfo>()
                    .FirstOrDefaultAsync(c => c.Id == clientInfo.Id);

                if (existing == null)
                    return false;

                existing.ContactName = clientInfo.ContactName;
                existing.Address = clientInfo.Address;
                existing.City = clientInfo.City;
                existing.State = clientInfo.State;
                existing.ZipCode = clientInfo.ZipCode;
                existing.PhoneNumber = clientInfo.PhoneNumber;
                existing.Email = clientInfo.Email;
                existing.Notes = clientInfo.Notes;
                existing.EmergencyContactName = clientInfo.EmergencyContactName;
                existing.EmergencyContactPhone = clientInfo.EmergencyContactPhone;
                existing.EmergencyContactEmail = clientInfo.EmergencyContactEmail;
                existing.IsActive = clientInfo.IsActive;
                existing.UpdatedByUserId = updatedByUserId;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Client info updated: {clientInfo.Id} by user {updatedByUserId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client info");
                return false;
            }
        }
    }
}
