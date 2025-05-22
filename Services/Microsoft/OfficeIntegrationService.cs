using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using JobTrackerApp.Models;
using System.IO;
using System.Threading.Tasks;

namespace JobTrackerApp.Services.Microsoft
{
    public class OfficeIntegrationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OfficeIntegrationService> _logger;

        public OfficeIntegrationService(IConfiguration configuration, ILogger<OfficeIntegrationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<string> GetAccessToken()
        {
            try
            {
                string clientId = _configuration["Microsoft:Graph:ClientId"] ?? 
                    throw new InvalidOperationException("Microsoft Graph ClientId not configured");
                string clientSecret = _configuration["Microsoft:Graph:ClientSecret"] ?? 
                    throw new InvalidOperationException("Microsoft Graph ClientSecret not configured");
                string tenantId = _configuration["Microsoft:Graph:TenantId"] ?? 
                    throw new InvalidOperationException("Microsoft Graph TenantId not configured");

                // Define the scope for the Microsoft Graph API
                string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

                // Create confidential client application
                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                    .Build();

                // Acquire token using client credentials flow
                AuthenticationResult result = await app
                    .AcquireTokenForClient(scopes)
                    .ExecuteAsync();

                return result.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Microsoft Graph access token");
                throw;
            }
        }

        public async Task<GraphServiceClient> GetGraphClient()
        {
            try
            {
                string accessToken = await GetAccessToken();

                // Initialize Graph client
                GraphServiceClient graphClient = new GraphServiceClient(
                    new DelegateAuthenticationProvider(requestMessage =>
                    {
                        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                        return Task.CompletedTask;
                    }));

                return graphClient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Microsoft Graph client");
                throw;
            }
        }

        public async Task<byte[]> GenerateExcelReport(IEnumerable<Job> jobs)
        {
            try
            {
                // For this simplified implementation, we'll create a basic Excel export
                // In a real application, you would use a library like EPPlus to create Excel files

                // Here we'll mock the Excel file generation with a byte array
                var graphClient = await GetGraphClient();

                // This is a placeholder - in a real implementation you would:
                // 1. Create an Excel file using a library like EPPlus
                // 2. Upload it to OneDrive/SharePoint if needed
                // 3. Return the file as a byte array

                _logger.LogInformation("Generated Excel report for {Count} jobs", jobs.Count());
                
                // Mock Excel data
                return Encoding.UTF8.GetBytes("This would be Excel binary data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate Excel report");
                throw;
            }
        }

        public async Task<byte[]> GenerateWordDocument(Job job)
        {
            try
            {
                // This is a placeholder - in a real implementation you would:
                // 1. Create a Word document using a library like DocX
                // 2. Upload it to OneDrive/SharePoint if needed
                // 3. Return the file as a byte array

                _logger.LogInformation("Generated Word document for job {JobId}: {JobName}", job.Id, job.Name);
                
                // Mock Word document data
                return Encoding.UTF8.GetBytes("This would be Word document binary data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate Word document for job {JobId}", job.Id);
                throw;
            }
        }
    }
}
