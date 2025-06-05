using System.Text;
using System.Text.Json;

namespace JobTracker.Services
{
    public class TextedlyService : IMessagingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TextedlyService> _logger;
        private readonly string? _apiKey;
        private readonly string? _apiSecret;
        private readonly string? _fromNumber;

        public TextedlyService(HttpClient httpClient, ILogger<TextedlyService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["TEXTEDLY_API_KEY"];
            _apiSecret = configuration["TEXTEDLY_API_SECRET"];
            _fromNumber = configuration["TEXTEDLY_FROM_NUMBER"];

            _httpClient.BaseAddress = new Uri("https://app.textedly.com/api/v1/");
            
            if (!string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_apiSecret))
            {
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiKey}:{_apiSecret}"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            }
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_apiSecret) || string.IsNullOrEmpty(_fromNumber))
            {
                _logger.LogWarning("Textedly credentials not configured. Cannot send SMS.");
                return false;
            }

            try
            {
                var payload = new
                {
                    to = phoneNumber,
                    from = _fromNumber,
                    body = message
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("messages", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"SMS sent successfully to {phoneNumber}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send SMS to {phoneNumber}: {response.StatusCode} - {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while sending SMS to {phoneNumber}");
                return false;
            }
        }

        public async Task<bool> SendVerificationSmsAsync(string phoneNumber, string verificationCode)
        {
            var message = $"Job Tracker verification code: {verificationCode}. This code expires in 10 minutes.";
            return await SendSmsAsync(phoneNumber, message);
        }

        public async Task<bool> SendJobUpdateSmsAsync(string phoneNumber, JobTracker.Models.Job job, string updateMessage)
        {
            var message = $"Job Update - {job.JobName}: {updateMessage}. Job #{job.JobNumber}";
            return await SendSmsAsync(phoneNumber, message);
        }

        public async Task<bool> SendScheduleReminderAsync(string phoneNumber, string employeeName, JobTracker.Models.Job job, DateTime scheduledTime)
        {
            var message = $"Hi {employeeName}, reminder: You're scheduled for {job.JobName} today at {scheduledTime:HH:mm}. Job #{job.JobNumber}";
            return await SendSmsAsync(phoneNumber, message);
        }
    }
}