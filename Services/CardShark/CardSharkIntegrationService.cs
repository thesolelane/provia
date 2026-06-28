using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JobTrackerApp.Models;

namespace JobTrackerApp.Services.CardShark
{
    public class CardSharkIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CardSharkIntegrationService> _logger;

        public CardSharkIntegrationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<CardSharkIntegrationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Configure base address and default headers
            string apiUrl = _configuration["CardShark:APIUrl"] ?? throw new InvalidOperationException("CardShark API URL not configured");
            _httpClient.BaseAddress = new Uri(apiUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Add API key authentication
            string apiKey = _configuration["CardShark:APIKey"] ?? throw new InvalidOperationException("CardShark API Key not configured");
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }

        // Method to sync employee time entries with CardShark
        public async Task<bool> SyncTimeEntries(IEnumerable<TimeEntry> timeEntries)
        {
            try
            {
                if (timeEntries == null || !timeEntries.Any())
                {
                    _logger.LogWarning("No time entries to sync with CardShark");
                    return false;
                }

                // Map time entries to CardShark format (this would depend on the CardShark API structure)
                var cardSharkEntries = timeEntries.Select(entry => new
                {
                    employeeId = entry.Employee?.EmployeeNumber,
                    jobCode = entry.Job?.JobNumber,
                    startTime = entry.ClockInTime.ToString("o"),
                    endTime = entry.ClockOutTime?.ToString("o"),
                    hours = entry.TotalHours,
                    notes = entry.Notes
                });

                var content = new StringContent(
                    JsonSerializer.Serialize(cardSharkEntries), 
                    Encoding.UTF8, 
                    "application/json");

                // Send to CardShark API
                var response = await _httpClient.PostAsync("/api/timeEntries", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Successfully synced {Count} time entries with CardShark", timeEntries.Count());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync time entries with CardShark");
                return false;
            }
        }

        // Method to get job data from CardShark
        public async Task<IEnumerable<Job>> GetJobsFromCardShark()
        {
            try
            {
                // Get jobs from CardShark API
                var response = await _httpClient.GetAsync("/api/jobs");
                response.EnsureSuccessStatusCode();

                // Read and deserialize the response
                var content = await response.Content.ReadAsStringAsync();
                var cardSharkJobs = JsonSerializer.Deserialize<List<object>>(content); // Replace with appropriate type
                
                // Map CardShark jobs to our Job model (this would depend on the CardShark data structure)
                // This is a simplified example
                var jobs = new List<Job>();
                
                // In a real implementation, you would map the CardShark data to your Job model
                
                _logger.LogInformation("Retrieved {Count} jobs from CardShark", jobs.Count);
                return jobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get jobs from CardShark");
                return Enumerable.Empty<Job>();
            }
        }

        // Method to get employee data from CardShark
        public async Task<IEnumerable<Employee>> GetEmployeesFromCardShark()
        {
            try
            {
                // Get employees from CardShark API
                var response = await _httpClient.GetAsync("/api/employees");
                response.EnsureSuccessStatusCode();

                // Read and deserialize the response
                var content = await response.Content.ReadAsStringAsync();
                var cardSharkEmployees = JsonSerializer.Deserialize<List<object>>(content); // Replace with appropriate type
                
                // Map CardShark employees to our Employee model (this would depend on the CardShark data structure)
                // This is a simplified example
                var employees = new List<Employee>();
                
                // In a real implementation, you would map the CardShark data to your Employee model
                
                _logger.LogInformation("Retrieved {Count} employees from CardShark", employees.Count);
                return employees;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get employees from CardShark");
                return Enumerable.Empty<Employee>();
            }
        }

        // Method to push a job update to CardShark
        public async Task<bool> PushJobUpdate(Job job)
        {
            try
            {
                // Map job to CardShark format (this would depend on the CardShark API structure)
                var cardSharkJob = new
                {
                    jobNumber = job.JobNumber,
                    name = job.Name,
                    location = job.Location,
                    status = job.Status,
                    startDate = job.StartDate.ToString("o"),
                    targetCompletionDate = job.TargetCompletionDate?.ToString("o"),
                    actualCompletionDate = job.ActualCompletionDate?.ToString("o"),
                    clientName = job.ClientName,
                    budget = job.Budget,
                    actualCost = job.ActualCost
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(cardSharkJob), 
                    Encoding.UTF8, 
                    "application/json");

                // Send to CardShark API
                var response = await _httpClient.PutAsync($"/api/jobs/{job.JobNumber}", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Successfully pushed job update to CardShark for job {JobId}", job.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to push job update to CardShark for job {JobId}", job.Id);
                return false;
            }
        }
    }
}
