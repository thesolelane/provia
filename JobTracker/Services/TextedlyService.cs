using System.Text;
using System.Text.Json;

namespace JobTracker.Services
{
    public class TwilioSmsService : IMessagingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TwilioSmsService> _logger;
        private readonly string? _accountSid;
        private readonly string? _authToken;
        private readonly string? _fromNumber;

        public TwilioSmsService(HttpClient httpClient, ILogger<TwilioSmsService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
            _authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
            _fromNumber = Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER");

            if (!string.IsNullOrEmpty(_accountSid) && !string.IsNullOrEmpty(_authToken))
            {
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_accountSid}:{_authToken}"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            }
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            if (string.IsNullOrEmpty(_accountSid) || string.IsNullOrEmpty(_authToken) || string.IsNullOrEmpty(_fromNumber))
            {
                _logger.LogWarning("Twilio credentials not configured. Cannot send SMS.");
                return false;
            }

            try
            {
                var payload = new Dictionary<string, string>
                {
                    {"From", _fromNumber},
                    {"To", phoneNumber},
                    {"Body", message}
                };

                var encodedContent = new FormUrlEncodedContent(payload);
                var apiUrl = $"https://api.twilio.com/2010-04-01/Accounts/{_accountSid}/Messages.json";

                var response = await _httpClient.PostAsync(apiUrl, encodedContent);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"SMS sent successfully to {phoneNumber} via Twilio");
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