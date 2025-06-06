using System.Text;
using System.Text.Json;

namespace JobTracker.Services
{
    public class ZapierSmsService : IMessagingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ZapierSmsService> _logger;
        private readonly IAIMessagingService _aiMessagingService;
        private readonly string? _zapierWebhookUrl;

        public ZapierSmsService(HttpClient httpClient, ILogger<ZapierSmsService> logger, 
            IAIMessagingService aiMessagingService, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _aiMessagingService = aiMessagingService;
            _zapierWebhookUrl = configuration["ZAPIER_SMS_WEBHOOK_URL"];
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            if (string.IsNullOrEmpty(_zapierWebhookUrl))
            {
                _logger.LogWarning("Zapier webhook URL not configured for SMS sending");
                return false;
            }

            try
            {
                var payload = new
                {
                    phone_number = phoneNumber,
                    message = message,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    source = "JobTracker"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_zapierWebhookUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"SMS request sent to Zapier webhook for {phoneNumber}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to send SMS via Zapier webhook to {phoneNumber}: {response.StatusCode} - {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while sending SMS via Zapier to {phoneNumber}");
                return false;
            }
        }

        public async Task<bool> SendVerificationSmsAsync(string phoneNumber, string verificationCode)
        {
            var message = await _aiMessagingService.GenerateVerificationMessageAsync("Team Member", verificationCode);
            return await SendSmsAsync(phoneNumber, message);
        }

        public async Task<bool> SendJobUpdateSmsAsync(string phoneNumber, JobTracker.Models.Job job, string updateMessage)
        {
            var message = await _aiMessagingService.GenerateJobUpdateMessageAsync(job, updateMessage);
            return await SendSmsAsync(phoneNumber, message);
        }

        public async Task<bool> SendScheduleReminderAsync(string phoneNumber, string employeeName, JobTracker.Models.Job job, DateTime scheduledTime)
        {
            var message = await _aiMessagingService.GenerateScheduleReminderAsync(employeeName, job, scheduledTime);
            return await SendSmsAsync(phoneNumber, message);
        }
    }
}