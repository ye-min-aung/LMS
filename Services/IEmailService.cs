namespace LMSPlatform.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetLink);
    Task SendWelcomeEmailAsync(string email, string fullName);
    Task SendEnrollmentConfirmationAsync(string email, string courseName);
    Task SendCertificateNotificationAsync(string email, string courseName, string certificateId);
    Task SendAssignmentFeedbackAsync(string email, string assignmentTitle, string status, string? feedback);
}
