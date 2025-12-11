using System.Net;
using System.Net.Mail;

namespace LMSPlatform.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        var subject = "Reset Your Password - LMS Platform";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #3498db 0%, #2980b9 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h1 style='color: white; margin: 0;'>Password Reset Request</h1>
                    </div>
                    <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;'>
                        <p>Hello,</p>
                        <p>We received a request to reset your password for your LMS Platform account.</p>
                        <p>Click the button below to reset your password:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetLink}' style='background: #ffb606; color: #333; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Reset Password</a>
                        </div>
                        <p>If you didn't request this password reset, you can safely ignore this email.</p>
                        <p>This link will expire in 24 hours.</p>
                        <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>
                        <p style='font-size: 12px; color: #666;'>If the button doesn't work, copy and paste this link into your browser:<br>{resetLink}</p>
                    </div>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }


    public async Task SendWelcomeEmailAsync(string email, string fullName)
    {
        var subject = "Welcome to LMS Platform!";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #3498db 0%, #2980b9 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h1 style='color: white; margin: 0;'>Welcome to LMS Platform!</h1>
                    </div>
                    <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;'>
                        <p>Hello {fullName},</p>
                        <p>Thank you for joining LMS Platform! We're excited to have you as part of our learning community.</p>
                        <p>Here's what you can do next:</p>
                        <ul>
                            <li>Browse our course catalog</li>
                            <li>Enroll in courses that interest you</li>
                            <li>Start learning at your own pace</li>
                        </ul>
                        <p>Happy learning!</p>
                        <p>The LMS Platform Team</p>
                    </div>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendEnrollmentConfirmationAsync(string email, string courseName)
    {
        var subject = $"Enrollment Confirmed - {courseName}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #27ae60 0%, #2ecc71 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h1 style='color: white; margin: 0;'>Enrollment Confirmed!</h1>
                    </div>
                    <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;'>
                        <p>Congratulations!</p>
                        <p>Your enrollment in <strong>{courseName}</strong> has been confirmed.</p>
                        <p>You now have full access to all course materials. Log in to your dashboard to start learning!</p>
                        <p>Happy learning!</p>
                    </div>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendCertificateNotificationAsync(string email, string courseName, string certificateId)
    {
        var subject = $"Certificate Earned - {courseName}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #f39c12 0%, #e67e22 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h1 style='color: white; margin: 0;'>🎉 Congratulations!</h1>
                    </div>
                    <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;'>
                        <p>You've successfully completed <strong>{courseName}</strong>!</p>
                        <p>Your certificate has been generated and is ready for download.</p>
                        <p><strong>Certificate ID:</strong> {certificateId}</p>
                        <p>Log in to your dashboard to view and download your certificate.</p>
                        <p>Keep up the great work!</p>
                    </div>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    public async Task SendAssignmentFeedbackAsync(string email, string assignmentTitle, string status, string? feedback)
    {
        var statusColor = status.ToLower() == "passed" ? "#27ae60" : "#e74c3c";
        var subject = $"Assignment Review - {assignmentTitle}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: {statusColor}; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                        <h1 style='color: white; margin: 0;'>Assignment Reviewed</h1>
                    </div>
                    <div style='background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px;'>
                        <p>Your assignment <strong>{assignmentTitle}</strong> has been reviewed.</p>
                        <p><strong>Status:</strong> <span style='color: {statusColor}; font-weight: bold;'>{status}</span></p>
                        {(string.IsNullOrEmpty(feedback) ? "" : $"<p><strong>Feedback:</strong> {feedback}</p>")}
                        <p>Log in to your dashboard to view the full details.</p>
                    </div>
                </div>
            </body>
            </html>";

        await SendEmailAsync(email, subject, body);
    }

    private async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"] ?? "localhost";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "25");
            var smtpUser = _configuration["Email:SmtpUser"];
            var smtpPassword = _configuration["Email:SmtpPassword"];
            var fromEmail = _configuration["Email:FromEmail"] ?? "noreply@lmsplatform.com";
            var fromName = _configuration["Email:FromName"] ?? "LMS Platform";

            using var client = new SmtpClient(smtpHost, smtpPort);
            
            if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPassword))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPassword);
                client.EnableSsl = true;
            }

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {Email}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", to);
            // In development, don't throw - just log
            // In production, you might want to throw or use a queue
        }
    }
}
