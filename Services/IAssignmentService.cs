using LMSPlatform.Models;

namespace LMSPlatform.Services;

public interface IAssignmentService
{
    // Assignment management
    Task<Assignment?> GetAssignmentByIdAsync(int assignmentId);
    Task<Assignment> CreateAssignmentAsync(CreateAssignmentModel model);
    Task<bool> UpdateAssignmentAsync(int assignmentId, UpdateAssignmentModel model);
    Task<bool> DeleteAssignmentAsync(int assignmentId);
    Task<IEnumerable<Assignment>> GetLessonAssignmentsAsync(int lessonId);
    
    // Assignment submission
    Task<AssignmentSubmission> SubmitAssignmentAsync(int userId, int assignmentId, IFormFile file);
    Task<AssignmentSubmission?> GetSubmissionAsync(int userId, int assignmentId);
    Task<AssignmentSubmission?> GetSubmissionByIdAsync(int submissionId);
    Task<IEnumerable<AssignmentSubmission>> GetAssignmentSubmissionsAsync(int assignmentId);
    Task<IEnumerable<AssignmentSubmission>> GetUserSubmissionsAsync(int userId);
    
    // Assignment review (Admin)
    Task<bool> ReviewSubmissionAsync(int submissionId, string status, string? feedback, int reviewerId, decimal? grade = null);
    Task<IEnumerable<AssignmentSubmission>> GetPendingSubmissionsAsync();
    Task<IEnumerable<AssignmentSubmission>> GetSubmissionsForReviewAsync(int reviewerId);
    
    // File management
    Task<string> UploadSubmissionFileAsync(IFormFile file, int userId, int assignmentId);
    Task<bool> DeleteSubmissionFileAsync(string fileUrl);
    Task<FileDownloadInfo?> GetSubmissionFileAsync(string fileUrl);
    
    // Validation
    Task<bool> CanUserSubmitAssignmentAsync(int userId, int assignmentId);
    Task<bool> IsFileTypeAllowedAsync(int assignmentId, string fileName);
    Task<bool> IsFileSizeValidAsync(int assignmentId, long fileSize);
}

// DTOs for assignment operations
public class CreateAssignmentModel
{
    public int? LessonID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public DateTime? DueDate { get; set; }
    public int MaxFileSize { get; set; } = 10485760; // 10MB
    public string AllowedFileTypes { get; set; } = "pdf,doc,docx,txt,zip";
    public bool IsRequired { get; set; }
}

public class UpdateAssignmentModel
{
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public DateTime? DueDate { get; set; }
    public int MaxFileSize { get; set; }
    public string AllowedFileTypes { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
}

public class FileDownloadInfo
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public long FileSize { get; set; }
}