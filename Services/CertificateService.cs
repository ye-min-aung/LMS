using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Services;

public class CertificateService : ICertificateService
{
    private readonly LMSDbContext _context;
    private readonly ILessonProgressService _progressService;
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(
        LMSDbContext context,
        ILessonProgressService progressService,
        ILogger<CertificateService> logger)
    {
        _context = context;
        _progressService = progressService;
        _logger = logger;
        
        // Configure QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    #region Certificate Generation

    public async Task<Certificate> GenerateCertificateAsync(int userId, int courseId)
    {
        // Check if certificate already exists
        var existingCertificate = await GetUserCourseCertificateAsync(userId, courseId);
        if (existingCertificate != null)
        {
            return existingCertificate;
        }

        // Verify eligibility
        if (!await IsEligibleForCertificateAsync(userId, courseId))
        {
            throw new InvalidOperationException("User is not eligible for certificate");
        }

        // Get user and course information
        var user = await _context.Users.FindAsync(userId);
        var course = await _context.Courses.FindAsync(courseId);
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId);

        if (user == null || course == null || enrollment == null)
        {
            throw new ArgumentException("User, course, or enrollment not found");
        }

        // Generate unique certificate ID
        var uniqueCertId = GenerateUniqueCertificateId();

        // Create certificate record
        var certificate = new Certificate
        {
            UserID = userId,
            CourseID = courseId,
            UniqueCertID = uniqueCertId,
            DateIssued = DateTime.UtcNow,
            CompletionDate = enrollment.CompletionDate ?? DateTime.UtcNow,
            FinalGrade = await CalculateFinalGradeAsync(userId, courseId)
        };

        _context.Certificates.Add(certificate);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Certificate generated: {CertificateId} for User {UserId}, Course {CourseId}", 
            certificate.CertificateID, userId, courseId);

        return certificate;
    }

    public async Task<byte[]> GenerateCertificatePdfAsync(int certificateId)
    {
        var certificate = await GetCertificateAsync(certificateId);
        if (certificate == null)
        {
            throw new ArgumentException("Certificate not found");
        }

        return await GenerateCertificatePdfAsync(certificate);
    }

    public async Task<byte[]> GenerateCertificatePdfAsync(Certificate certificate)
    {
        // Get related data
        var user = await _context.Users.FindAsync(certificate.UserID);
        var course = await _context.Courses.FindAsync(certificate.CourseID);

        if (user == null || course == null)
        {
            throw new InvalidOperationException("User or course not found for certificate");
        }

        var generationModel = new CertificateGenerationModel
        {
            UserId = certificate.UserID,
            CourseId = certificate.CourseID,
            StudentName = user.FullName,
            CourseName = course.GetTitle(),
            CompletionDate = certificate.CompletionDate,
            FinalGrade = certificate.FinalGrade,
            InstructorName = "LMS Platform", // Could be made configurable
            CourseDuration = TimeSpan.FromDays(30) // Could be calculated from actual course data
        };

        return GeneratePdfDocument(certificate, generationModel);
    }

    #endregion

    #region Certificate Management

    public async Task<Certificate?> GetCertificateAsync(int certificateId)
    {
        return await _context.Certificates
            .Include(c => c.User)
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.CertificateID == certificateId);
    }

    public async Task<Certificate?> GetUserCourseCertificateAsync(int userId, int courseId)
    {
        return await _context.Certificates
            .Include(c => c.User)
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.UserID == userId && c.CourseID == courseId);
    }

    public async Task<IEnumerable<Certificate>> GetUserCertificatesAsync(int userId)
    {
        return await _context.Certificates
            .Where(c => c.UserID == userId)
            .Include(c => c.Course)
            .OrderByDescending(c => c.IssuedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Certificate>> GetCourseCertificatesAsync(int courseId)
    {
        return await _context.Certificates
            .Where(c => c.CourseID == courseId)
            .Include(c => c.User)
            .OrderByDescending(c => c.IssuedDate)
            .ToListAsync();
    }

    #endregion

    #region Certificate Validation

    public async Task<bool> IsEligibleForCertificateAsync(int userId, int courseId)
    {
        // Check if user is enrolled and course is completed
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId);

        if (enrollment == null || !enrollment.IsActive)
        {
            return false;
        }

        // Check if course is completed
        var isCompleted = await _progressService.IsCourseCompletedAsync(enrollment.EnrollmentID);
        if (!isCompleted)
        {
            return false;
        }

        // Check if all required quizzes are passed
        var requiredQuizzes = await _context.Quizzes
            .Where(q => q.Lesson!.Module.CourseID == courseId && q.RequiredToUnlock)
            .ToListAsync();

        foreach (var quiz in requiredQuizzes)
        {
            var latestAttempt = await _context.QuizAttempts
                .Where(qa => qa.UserID == userId && qa.QuizID == quiz.QuizID)
                .OrderByDescending(qa => qa.StartedAt)
                .FirstOrDefaultAsync();

            if (latestAttempt == null || !latestAttempt.Passed)
            {
                return false;
            }
        }

        // Check if all required assignments are submitted and passed
        var requiredAssignments = await _context.Assignments
            .Where(a => a.Lesson!.Module.CourseID == courseId && a.IsRequired)
            .ToListAsync();

        foreach (var assignment in requiredAssignments)
        {
            var submission = await _context.AssignmentSubmissions
                .FirstOrDefaultAsync(asub => asub.UserID == userId && asub.AssignmentID == assignment.AssignmentID);

            if (submission == null || submission.Status != SubmissionStatus.Passed)
            {
                return false;
            }
        }

        return true;
    }

    public async Task<Certificate?> ValidateCertificateAsync(string uniqueCertId)
    {
        return await _context.Certificates
            .Include(c => c.User)
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.UniqueCertID == uniqueCertId);
    }

    public async Task<bool> IsCertificateValidAsync(string uniqueCertId)
    {
        var certificate = await ValidateCertificateAsync(uniqueCertId);
        return certificate != null;
    }

    #endregion

    #region Automatic Certificate Generation

    public async Task CheckAndGenerateCertificateAsync(int userId, int courseId)
    {
        if (await ShouldGenerateCertificateAsync(userId, courseId))
        {
            await GenerateCertificateAsync(userId, courseId);
        }
    }

    public async Task<bool> ShouldGenerateCertificateAsync(int userId, int courseId)
    {
        // Check if certificate already exists
        var existingCertificate = await GetUserCourseCertificateAsync(userId, courseId);
        if (existingCertificate != null)
        {
            return false;
        }

        // Check eligibility
        return await IsEligibleForCertificateAsync(userId, courseId);
    }

    #endregion

    #region Private Helper Methods

    private string GenerateUniqueCertificateId()
    {
        // Generate a unique certificate ID using timestamp and random components
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N")[..8].ToUpper();
        return $"CERT-{timestamp}-{randomPart}";
    }

    private async Task<decimal> CalculateFinalGradeAsync(int userId, int courseId)
    {
        // Get course progress percentage
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId);

        if (enrollment == null)
        {
            return 0;
        }

        var progressPercentage = await _progressService.CalculateCourseProgressAsync(enrollment.EnrollmentID);

        // Get average quiz scores
        var quizScores = await _context.QuizAttempts
            .Where(qa => qa.UserID == userId && qa.Quiz.Lesson!.Module.CourseID == courseId && qa.Passed)
            .GroupBy(qa => qa.QuizID)
            .Select(g => g.OrderByDescending(qa => qa.Score).First().Score)
            .ToListAsync();

        var averageQuizScore = quizScores.Any() ? quizScores.Average() : 100;

        // Calculate final grade (70% progress, 30% quiz average)
        var finalGrade = (progressPercentage * 0.7m) + (averageQuizScore * 0.3m);
        return Math.Round(finalGrade, 2);
    }

    private byte[] GeneratePdfDocument(Certificate certificate, CertificateGenerationModel model)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(50);
                page.DefaultTextStyle(x => x.FontSize(12).FontFamily(Fonts.Arial));

                page.Content().Column(column =>
                {
                    // Header
                    column.Item().AlignCenter().Text("CERTIFICATE OF COMPLETION")
                        .FontSize(28).Bold().FontColor(Colors.Blue.Darken2);

                    column.Item().PaddingVertical(20);

                    // Decorative line
                    column.Item().AlignCenter().Width(300).Height(2).Background(Colors.Blue.Lighten2);

                    column.Item().PaddingVertical(30);

                    // Main content
                    column.Item().AlignCenter().Text("This is to certify that")
                        .FontSize(16).Italic();

                    column.Item().PaddingVertical(10);

                    column.Item().AlignCenter().Text(model.StudentName)
                        .FontSize(24).Bold().FontColor(Colors.Blue.Darken1);

                    column.Item().PaddingVertical(20);

                    column.Item().AlignCenter().Text("has successfully completed the course")
                        .FontSize(16).Italic();

                    column.Item().PaddingVertical(10);

                    column.Item().AlignCenter().Text(model.CourseName)
                        .FontSize(20).Bold().FontColor(Colors.Blue.Darken1);

                    column.Item().PaddingVertical(20);

                    // Course details
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Column(col =>
                        {
                            col.Item().Text("Completion Date").FontSize(12).Bold();
                            col.Item().Text(model.CompletionDate.ToString("MMMM dd, yyyy")).FontSize(14);
                        });

                        row.RelativeItem().AlignCenter().Column(col =>
                        {
                            col.Item().Text("Final Grade").FontSize(12).Bold();
                            col.Item().Text($"{model.FinalGrade:F1}%").FontSize(14);
                        });
                    });

                    column.Item().PaddingVertical(30);

                    // Signature area
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Column(col =>
                        {
                            col.Item().Width(200).Height(1).Background(Colors.Grey.Medium);
                            col.Item().PaddingTop(5).Text("Instructor").FontSize(12);
                            col.Item().Text(model.InstructorName).FontSize(14).Bold();
                        });

                        row.RelativeItem().AlignCenter().Column(col =>
                        {
                            col.Item().Width(200).Height(1).Background(Colors.Grey.Medium);
                            col.Item().PaddingTop(5).Text("Date Issued").FontSize(12);
                            col.Item().Text(certificate.IssuedDate.ToString("MMMM dd, yyyy")).FontSize(14).Bold();
                        });
                    });

                    column.Item().PaddingVertical(20);

                    // Certificate ID
                    column.Item().AlignCenter().Text($"Certificate ID: {certificate.UniqueCertID}")
                        .FontSize(10).FontColor(Colors.Grey.Darken1);
                });

                // Footer
                page.Footer().AlignCenter().Text("LMS Platform - Learning Management System")
                    .FontSize(10).FontColor(Colors.Grey.Medium);
            });
        });

        return document.GeneratePdf();
    }

    #endregion
}