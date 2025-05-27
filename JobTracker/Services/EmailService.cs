using System.Net;
using System.Net.Mail;
using System.Text;

namespace JobTracker.Services
{
    public interface IEmailService
    {
        Task<bool> SendVerificationEmailAsync(string email, string firstName, string verificationCode, string temporaryPassword);
        Task<bool> SendPasswordResetEmailAsync(string email, string firstName, string resetCode);
        Task<bool> SendWelcomeEmailAsync(string email, string firstName, string companyName);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendVerificationEmailAsync(string email, string firstName, string verificationCode, string temporaryPassword)
        {
            try
            {
                var subject = "Welcome to Job Tracker Pro - Verify Your Account";
                var body = GenerateVerificationEmailBody(firstName, verificationCode, temporaryPassword);
                
                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string firstName, string resetCode)
        {
            try
            {
                var subject = "Job Tracker Pro - Password Reset Request";
                var body = GeneratePasswordResetEmailBody(firstName, resetCode);
                
                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(string email, string firstName, string companyName)
        {
            try
            {
                var subject = "Welcome to Job Tracker Pro!";
                var body = GenerateWelcomeEmailBody(firstName, companyName);
                
                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to {Email}", email);
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string email, string subject, string body)
        {
            try
            {
                // Check if SendGrid API key is available
                var sendGridApiKey = _configuration["SENDGRID_API_KEY"];
                if (!string.IsNullOrEmpty(sendGridApiKey))
                {
                    return await SendEmailViaSendGrid(email, subject, body, sendGridApiKey);
                }

                // Fallback to SMTP if configured
                var smtpHost = _configuration["SmtpSettings:Host"];
                var smtpPort = _configuration.GetValue<int>("SmtpSettings:Port", 587);
                var smtpUsername = _configuration["SmtpSettings:Username"];
                var smtpPassword = _configuration["SmtpSettings:Password"];

                if (!string.IsNullOrEmpty(smtpHost) && !string.IsNullOrEmpty(smtpUsername))
                {
                    return await SendEmailViaSmtp(email, subject, body, smtpHost, smtpPort, smtpUsername, smtpPassword);
                }

                _logger.LogWarning("No email service configured. Email not sent to {Email}", email);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
                return false;
            }
        }

        private async Task<bool> SendEmailViaSendGrid(string email, string subject, string body, string apiKey)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var fromEmail = "noreply@smartjobtracker.com"; // Professional system email
                var fromName = "Smart Job Tracker";

                var emailData = new
                {
                    personalizations = new[]
                    {
                        new
                        {
                            to = new[] { new { email, name = "" } },
                            subject
                        }
                    },
                    from = new { email = fromEmail, name = fromName },
                    content = new[]
                    {
                        new
                        {
                            type = "text/html",
                            value = body
                        }
                    }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(emailData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.sendgrid.com/v3/mail/send", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SendGrid email sent successfully to {Email}", email);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("SendGrid API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendGrid email failed for {Email}", email);
                return false;
            }
        }

        private async Task<bool> SendEmailViaSmtp(string email, string subject, string body, string host, int port, string username, string password)
        {
            try
            {
                using var client = new SmtpClient(host, port);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(username, password);

                var message = new MailMessage
                {
                    From = new MailAddress(username, "Job Tracker Pro"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(email);
                await client.SendMailAsync(message);
                
                _logger.LogInformation("SMTP email sent successfully to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP email failed for {Email}", email);
                return false;
            }
        }

        private string GenerateVerificationEmailBody(string firstName, string verificationCode, string temporaryPassword)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; color: #2c3e50; margin-bottom: 30px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 10px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .credentials {{ background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #007bff; }}
        .button {{ display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; text-align: center; }}
        .brand {{ color: #007bff; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔨 Smart Job Tracker</h1>
            <p style='margin: 0; opacity: 0.9;'>Professional Construction Management Platform</p>
        </div>
        
        <div class='content'>
            <p>Hello {firstName},</p>
            
            <p>Welcome to <span class='brand'>Smart Job Tracker</span>! You've been added as a team member and your account is ready to be activated.</p>
            
            <div class='credentials'>
                <h3>🔐 Account Activation Details</h3>
                <p><strong>Verification Code:</strong> <code style='background: #e9ecef; padding: 2px 6px; border-radius: 3px; font-family: monospace;'>{verificationCode}</code></p>
                <p><strong>Temporary Password:</strong> <code style='background: #e9ecef; padding: 2px 6px; border-radius: 3px; font-family: monospace;'>{temporaryPassword}</code></p>
                <p><small><em>This code expires in 24 hours for security</em></small></p>
            </div>
            
            <p><strong>Getting Started:</strong></p>
            <ol>
                <li>Visit your company's Job Tracker dashboard</li>
                <li>Enter your verification code</li>
                <li>Log in with your temporary password</li>
                <li>Create your personal secure password</li>
                <li>Start managing construction projects!</li>
            </ol>
            
            <p><strong>Security Notice:</strong> You must change your temporary password during your first login. This ensures your account stays secure.</p>
        </div>
        
        <div class='footer'>
            <p><strong>Smart Job Tracker</strong> - Construction Management Platform</p>
            <p>This is an automated message from our system. Please do not reply to this email.</p>
            <p>If you didn't expect this email, please contact your company administrator.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GeneratePasswordResetEmailBody(string firstName, string resetCode)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; color: #dc3545; margin-bottom: 30px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .reset-code {{ background-color: #fff3cd; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #ffc107; }}
        .button {{ display: inline-block; background-color: #dc3545; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔑 Password Reset Request</h1>
        </div>
        
        <div class='content'>
            <p>Hello {firstName},</p>
            
            <p>We received a request to reset your password for Job Tracker Pro.</p>
            
            <div class='reset-code'>
                <h3>🔓 Password Reset Code</h3>
                <p><strong>Reset Code:</strong> {resetCode}</p>
                <p><em>This code expires in 30 minutes</em></p>
            </div>
            
            <p><strong>To reset your password:</strong></p>
            <ol>
                <li>Click the reset link below</li>
                <li>Enter the reset code above</li>
                <li>Create your new secure password</li>
            </ol>
            
            <a href='#' class='button'>Reset Password</a>
            
            <p><strong>Security Note:</strong> If you didn't request this password reset, please ignore this email and contact your administrator.</p>
        </div>
        
        <div class='footer'>
            <p>This email was sent by Job Tracker Pro Construction Management System.</p>
            <p>For security reasons, this reset code will expire in 30 minutes.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateWelcomeEmailBody(string firstName, string companyName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; color: #28a745; margin-bottom: 30px; }}
        .content {{ line-height: 1.6; color: #333; }}
        .features {{ background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Welcome to the Team!</h1>
        </div>
        
        <div class='content'>
            <p>Hello {firstName},</p>
            
            <p>Welcome to <strong>{companyName}</strong>! Your account has been successfully verified and you're now part of our construction management team.</p>
            
            <div class='features'>
                <h3>🚀 What you can do now:</h3>
                <ul>
                    <li>Track job progress across multiple construction phases</li>
                    <li>Manage time entries and work schedules</li>
                    <li>Upload and organize project photos</li>
                    <li>Coordinate with subcontractors and team members</li>
                    <li>Access building code guidance and compliance tools</li>
                </ul>
            </div>
            
            <p>Get started by logging into your account and exploring the dashboard. If you have any questions, don't hesitate to reach out to your team administrator.</p>
            
            <p>Happy building!</p>
        </div>
        
        <div class='footer'>
            <p>This email was sent by Job Tracker Pro Construction Management System.</p>
            <p>You're receiving this because you've been added to {companyName}'s team.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}