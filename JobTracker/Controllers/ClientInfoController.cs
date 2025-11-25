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
    public class ClientInfoController : ControllerBase
    {
        private readonly IClientInfoService _clientInfoService;
        private readonly JobTrackerContext _context;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<ClientInfoController> _logger;

        public ClientInfoController(
            IClientInfoService clientInfoService,
            JobTrackerContext context,
            ITenantContext tenantContext,
            ILogger<ClientInfoController> logger)
        {
            _clientInfoService = clientInfoService;
            _context = context;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        /// <summary>
        /// Get client info with role-based filtering (no billing data)
        /// </summary>
        [HttpGet("{clientId}")]
        public async Task<ActionResult<ClientInfoDto>> GetClientInfo(int clientId)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                var companyId = _tenantContext.GetCurrentCompanyId();

                var clientInfo = await _clientInfoService.GetClientInfoAsync(clientId, user.Role, companyId);

                if (clientInfo == null)
                    return NotFound();

                _logger.LogInformation($"User {user.Id} (role {user.Role}) accessed client info {clientId}");
                return Ok(clientInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving client info");
                return StatusCode(500, new { message = "Error retrieving client information" });
            }
        }

        /// <summary>
        /// Get all clients for company with role-based filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ClientInfoDto>>> GetAllClients()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                var companyId = _tenantContext.GetCurrentCompanyId();

                var clients = await _clientInfoService.GetAllClientsAsync(user.Role, companyId);

                return Ok(new
                {
                    count = clients.Count,
                    clients = clients
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving clients");
                return StatusCode(500, new { message = "Error retrieving client list" });
            }
        }

        /// <summary>
        /// Create new client info (admin/foreman only)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ClientInfoDto>> CreateClientInfo([FromBody] ClientInfo clientInfo)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                // Only Admin (1510) and Foreman (1520) can create client info
                if (user.Role != 1510 && user.Role != 1520)
                    return Forbid("Only admins and foremans can create client information");

                var companyId = _tenantContext.GetCurrentCompanyId();
                clientInfo.CompanyId = companyId;

                var success = await _clientInfoService.CreateClientInfoAsync(clientInfo, user.Id);

                if (!success)
                    return BadRequest("Failed to create client information");

                _logger.LogInformation($"Client info created by user {user.Id}");
                return Ok(ClientInfoDto.FromModel(clientInfo, user.Role));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating client info");
                return StatusCode(500, new { message = "Error creating client information" });
            }
        }

        /// <summary>
        /// Update client info (admin/foreman only)
        /// </summary>
        [HttpPut("{clientId}")]
        public async Task<ActionResult<ClientInfoDto>> UpdateClientInfo(int clientId, [FromBody] ClientInfo clientInfo)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

                if (user == null)
                    return Unauthorized();

                // Only Admin (1510) and Foreman (1520) can update client info
                if (user.Role != 1510 && user.Role != 1520)
                    return Forbid("Only admins and foremans can update client information");

                clientInfo.Id = clientId;
                var success = await _clientInfoService.UpdateClientInfoAsync(clientInfo, user.Id);

                if (!success)
                    return BadRequest("Failed to update client information");

                _logger.LogInformation($"Client info updated by user {user.Id}");
                return Ok(ClientInfoDto.FromModel(clientInfo, user.Role));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client info");
                return StatusCode(500, new { message = "Error updating client information" });
            }
        }
    }
}
