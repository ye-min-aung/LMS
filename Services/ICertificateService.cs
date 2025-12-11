using LMSPlatform.Models;

namespace LMSPlatform.Services;

public interface ICertificateService
{
    // Certificate generation
    Task<Certificate> GenerateCertificateAsync(int userId, int courseId);
    Task<byte[]> GenerateCertificatePdfAsync(int certificateId);
    Task<byte[]> GenerateCertificatePdfAsync(Certificate certificate);
    
    // Certificate management
    Task<Certificate?> GetCertificateAsync(int certificateId);
    Task<Certificate?> GetUserCourseCertificateAsync(int userId, int courseId);
    Task<IEnumerable<Certificate>> GetUserCertificatesAsync(int userId);
    Task<IEnumerable<Certificate>> GetCourseCertificatesAsync(int courseId);
    
    // Certificate validation
    Task<bool> IsEligibleForCertificateAsync(int userId, int courseId);
    Task<Certificate?> ValidateCertificateAsync(string uniqueCertId);
    Task<bool> IsCertificateValidAsync(string uniqueCertId);
    
    // Automatic certificate generation
    Task CheckAndGenerateCertificateAsync(int userId, int courseId);
    Task<bool> ShouldGenerateCertificateAsync(int userId, int courseId);
}

// DTOs for certificate operations
public class CertificateGenerationModel
{
    public int UserId { get; set; }
    public int CourseId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public DateTime CompletionDate { get; set; }
    public decimal FinalGrade { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public TimeSpan CourseDuration { get; set; }
}

public class CertificateValidationResult
{
    public bool IsValid { get; set; }
    public Certificate? Certificate { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ValidationDate { get; set; }
}