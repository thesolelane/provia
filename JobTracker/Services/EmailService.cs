using System.Net.Mail;
using System.Net;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace JobTracker.Services
{
    public interface IEmailService
    {
        Task<bool> SendLocationViolationEmailAsync(string toEmail, string userName, string jobName, double distance, double allowedDistance);
        Task<bool> SendAdminLocationAlertAsync(string toEmail, string adminName, string workerName, string jobName, double distance, double allowedDistance);
        Task<bool> SendEmailAsync(string to, string subject, string body);
        Task<bool> SendHtmlEmailAsync(string to, string subject, string htmlBody, string plainTextBody);
        Task<bool> SendVerificationEmailAsync(string email, string verificationCode, string firstName, string? companyName = null);
        Task<bool> SendWelcomeEmailAsync(string email, string firstName, string tempPassword);
        Task<bool> SendPasswordResetEmailAsync(string email, string resetCode, string firstName);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly ISendGridClient? _sendGridClient;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _logger = logger;
            _fromEmail = configuration["EmailSettings:FromEmail"] ?? "noreply@smartjobtracker.com";
            _fromName = configuration["EmailSettings:FromName"] ?? "Smart Job Tracker";
            
            var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
            _logger.LogInformation($"SendGrid API Key found: {!string.IsNullOrEmpty(apiKey)}");
            if (!string.IsNullOrEmpty(apiKey))
            {
                _sendGridClient = new SendGridClient(apiKey);
                _logger.LogInformation("SendGrid client initialized successfully");
            }
            else
            {
                _logger.LogWarning("SendGrid API Key not found - emails will be simulated");
            }
        }

        public async Task<bool> SendLocationViolationEmailAsync(string toEmail, string userName, string jobName, double distance, double allowedDistance)
        {
            try
            {
                var subject = "Location Alert - Auto Clock-Out";
                var htmlContent = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background: #f8d7da; color: #721c24; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                            <h2 style='margin: 0; color: #721c24;'>⚠️ Location Violation Alert</h2>
                        </div>
                        
                        <p>Hello {userName},</p>
                        
                        <p>You have been automatically clocked out due to a location violation:</p>
                        
                        <div style='background: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <strong>Details:</strong><br>
                            • Job Site: {jobName}<br>
                            • Your Distance: {distance} feet from job site<br>
                            • Allowed Distance: {allowedDistance} feet<br>
                            • Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                        </div>
                        
                        <p><strong>Next Steps:</strong></p>
                        <ul>
                            <li>Return to the job site to clock back in</li>
                            <li>Or select a different location (material run, lunch break)</li>
                            <li>Contact your supervisor if you need assistance</li>
                        </ul>
                        
                        <p>This is an automated message from the Job Tracker system.</p>
                    </div>";

                var plainTextContent = $"Location Alert: You have been automatically clocked out. You were {distance} feet from {jobName} (allowed: {allowedDistance} feet). Return to the job site or select a different location to continue working.";

                return await SendEmailInternalAsync(toEmail, subject, htmlContent, plainTextContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send location violation email");
                return false;
            }
        }

        public async Task<bool> SendAdminLocationAlertAsync(string toEmail, string adminName, string workerName, string jobName, double distance, double allowedDistance)
        {
            try
            {
                var subject = $"Worker Location Alert - {workerName}";
                var htmlContent = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background: #f8d7da; color: #721c24; padding: 20px; border-radius: 8px; margin-bottom: 20px;'>
                            <h2 style='margin: 0; color: #721c24;'>🚨 Worker Location Alert</h2>
                        </div>
                        
                        <p>Hello {adminName},</p>
                        
                        <p>A worker has been automatically clocked out due to a location violation:</p>
                        
                        <div style='background: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <strong>Details:</strong><br>
                            • Worker: {workerName}<br>
                            • Job Site: {jobName}<br>
                            • Distance: {distance} feet from job site<br>
                            • Allowed: {allowedDistance} feet<br>
                            • Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                        </div>
                        
                        <p>The worker has been notified and instructed to return to the job site or update their location.</p>
                        
                        <p>You may want to follow up to ensure proper work continuation.</p>
                    </div>";

                var plainTextContent = $"Admin Alert: {workerName} was automatically clocked out for being {distance} feet from {jobName} (allowed: {allowedDistance} feet) at {DateTime.Now:yyyy-MM-dd HH:mm:ss}.";

                return await SendEmailInternalAsync(toEmail, subject, htmlContent, plainTextContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send admin location alert email");
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            return await SendEmailInternalAsync(to, subject, body, body);
        }

        public async Task<bool> SendHtmlEmailAsync(string to, string subject, string htmlBody, string plainTextBody)
        {
            return await SendEmailInternalAsync(to, subject, htmlBody, plainTextBody);
        }

        public async Task<bool> SendVerificationEmailAsync(string email, string verificationCode, string firstName, string? companyName = null)
        {
            try
            {
                var subject = "Email Verification - Job Tracker";
                var body = $"Hello {firstName}, your verification code is: {verificationCode}";
                _logger.LogInformation($"Verification email would be sent to {email} with code: {verificationCode}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send verification email to {email}");
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string email, string firstName, string tempPassword)
        {
            try
            {
                var subject = "Welcome to Job Tracker";
                var body = $"Hello {firstName}, welcome to Job Tracker. Your temporary password is: {tempPassword}";
                _logger.LogInformation($"Welcome email would be sent to {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send welcome email to {email}");
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetCode, string firstName)
        {
            try
            {
                var subject = "Password Reset - Job Tracker";
                var body = $"Hello {firstName}, your password reset code is: {resetCode}";
                _logger.LogInformation($"Password reset email would be sent to {email} with code: {resetCode}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send password reset email to {email}");
                return false;
            }
        }

        private async Task<bool> SendEmailInternalAsync(string toEmail, string subject, string htmlContent, string plainTextContent)
        {
            try
            {
                if (_sendGridClient == null)
                {
                    _logger.LogWarning($"SendGrid not configured. Email would be sent to {toEmail}: {subject}");
                    return false;
                }

                var from = new EmailAddress(_fromEmail, _fromName);
                var to = new EmailAddress(toEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

                var response = await _sendGridClient.SendEmailAsync(msg);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Email successfully sent to {toEmail}: {subject}");
                    return true;
                }
                else
                {
                    _logger.LogError($"Failed to send email to {toEmail}. Status: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}");
                return false;
            }
        }
    }
}