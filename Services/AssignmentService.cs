using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Services;

public class AssignmentService : IAssignmentService
{
    private readonly LMSDbContext _context;
    private readonly IPrerequisiteService _prerequisiteService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AssignmentService> _logger;

    public AssignmentService(
        LMSDbContext context,
        IPrerequisiteService prerequisiteService,
        IWebHostEnvironment environment,
        ILogger<AssignmentService> logger)
    {
        _context = context;
        _prerequisiteService = prerequisiteService;
        _environment = environment;
        _logger = logger;
    }

    #region Assignment Management

    public async Task<Assignment?> GetAssignmentByIdAsync(int assignmentId)
    {
        return await _context.Assignments
            .Include(a => a.Lesson)
                .ThenInclude(l => l!.Module)
                    .ThenInclude(m => m.Course)
            .Include(a => a.AssignmentSubmissions)
                .ThenInclude(asub => asub.User)
            .FirstOrDefaultAsync(a => a.AssignmentID == assignmentId);
    }

    public async Task<Assignment> CreateAssignmentAsync(CreateAssignmentModel model)
    {
        var assignment = new Assignment
        {
            LessonID = model.LessonID,
            Title = model.Title,
            Instructions = model.Instructions,
            DueDate = model.DueDate,
            MaxFileSize = model.MaxFileSize,
            AllowedFileTypes = model.AllowedFileTypes,
            IsRequired = model.IsRequired
        };

        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Assignment created: {AssignmentId} - {Title}", assignment.AssignmentID, assignment.Title);
        return assignment;
    }

    public async Task<bool> UpdateAssignmentAsync(int assignmentId, UpdateAssignmentModel model)
    {
        var assignment = await _context.Assignments.FindAsync(assignmentId);
        if (assignment == null)
        {
            return false;
        }

        assignment.Title = model.Title;
        assignment.Instructions = model.Instructions;
        assignment.DueDate = model.DueDate;
        assignment.MaxFileSize = model.MaxFileSize;
        assignment.AllowedFileTypes = model.AllowedFileTypes;
        assignment.IsRequired = model.IsRequired;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Assignment updated: {AssignmentId}", assignmentId);
        return true;
    }

    public async Task<bool> DeleteAssignmentAsync(int assignmentId)
    {
        var assignment = await _context.Assignments.FindAsync(assignmentId);
        if (assignment == null)
        {
            return false;
        }

        _context.Assignments.Remove(assignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Assignment deleted: {AssignmentId}", assignmentId);
        return true;
    }

    public async Task<IEnumerable<Assignment>> GetLessonAssignmentsAsync(int lessonId)
    {
        return await _context.Assignments
            .Where(a => a.LessonID == lessonId)
            .Include(a => a.AssignmentSubmissions)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    #endregion

    #region Assignment Submission

    public async Task<AssignmentSubmission> SubmitAssignmentAsync(int userId, int assignmentId, IFormFile file)
    {
        // Validate user can submit
        if (!await CanUserSubmitAssignmentAsync(userId, assignmentId))
        {
            throw new InvalidOperationException("User cannot submit this assignment");
        }

        // Validate file
        if (!await IsFileTypeAllowedAsync(assignmentId, file.FileName))
        {
            throw new ArgumentException("File type not allowed");
        }

        if (!await IsFileSizeValidAsync(assignmentId, file.Length))
        {
            throw new ArgumentException("File size exceeds maximum allowed size");
        }

        // Check if user already has a submission
        var existingSubmission = await GetSubmissionAsync(userId, assignmentId);
        if (existingSubmission != null)
        {
            // Update existing submission
            var newFileUrl = await UploadSubmissionFileAsync(file, userId, assignmentId);
            
            // Delete old file
            if (!string.IsNullOrEmpty(existingSubmission.SubmissionFileUrl))
            {
                await DeleteSubmissionFileAsync(existingSubmission.SubmissionFileUrl);
            }

            existingSubmission.SubmissionFileUrl = newFileUrl;
            existingSubmission.OriginalFileName = file.FileName;
            existingSubmission.SubmittedAt = DateTime.UtcNow;
            existingSubmission.Status = SubmissionStatus.Pending;
            existingSubmission.ReviewedAt = null;
            existingSubmission.ReviewedBy = null;
            existingSubmission.AdminMark = null;
            existingSubmission.Grade = null;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Assignment submission updated: {SubmissionId} by User {UserId}", 
                existingSubmission.SubmissionID, userId);

            return existingSubmission;
        }
        else
        {
            // Create new submission
            var fileUrl = await UploadSubmissionFileAsync(file, userId, assignmentId);

            var submission = new AssignmentSubmission
            {
                AssignmentID = assignmentId,
                UserID = userId,
                SubmissionFileUrl = fileUrl,
                OriginalFileName = file.FileName,
                Status = SubmissionStatus.Pending
            };

            _context.AssignmentSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Assignment submission created: {SubmissionId} by User {UserId}", 
                submission.SubmissionID, userId);

            return submission;
        }
    }

    public async Task<AssignmentSubmission?> GetSubmissionAsync(int userId, int assignmentId)
    {
        return await _context.AssignmentSubmissions
            .Include(asub => asub.Assignment)
            .Include(asub => asub.User)
            .Include(asub => asub.ReviewedByUser)
            .FirstOrDefaultAsync(asub => asub.UserID == userId && asub.AssignmentID == assignmentId);
    }

    public async Task<AssignmentSubmission?> GetSubmissionByIdAsync(int submissionId)
    {
        return await _context.AssignmentSubmissions
            .Include(asub => asub.Assignment)
                .ThenInclude(a => a!.Lesson)
                    .ThenInclude(l => l!.Module)
                        .ThenInclude(m => m.Course)
            .Include(asub => asub.User)
            .Include(asub => asub.ReviewedByUser)
            .FirstOrDefaultAsync(asub => asub.SubmissionID == submissionId);
    }

    public async Task<IEnumerable<AssignmentSubmission>> GetAssignmentSubmissionsAsync(int assignmentId)
    {
        return await _context.AssignmentSubmissions
            .Where(asub => asub.AssignmentID == assignmentId)
            .Include(asub => asub.User)
            .Include(asub => asub.ReviewedByUser)
            .OrderByDescending(asub => asub.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AssignmentSubmission>> GetUserSubmissionsAsync(int userId)
    {
        return await _context.AssignmentSubmissions
            .Where(asub => asub.UserID == userId)
            .Include(asub => asub.Assignment)
                .ThenInclude(a => a!.Lesson)
                    .ThenInclude(l => l!.Module)
                        .ThenInclude(m => m.Course)
            .Include(asub => asub.ReviewedByUser)
            .OrderByDescending(asub => asub.SubmittedAt)
            .ToListAsync();
    }

    #endregion

    #region Assignment Review

    public async Task<bool> ReviewSubmissionAsync(int submissionId, string status, string? feedback, int reviewerId, decimal? grade = null)
    {
        var submission = await _context.AssignmentSubmissions.FindAsync(submissionId);
        if (submission == null)
        {
            return false;
        }

        submission.MarkAsReviewed(status, feedback, reviewerId, grade);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Assignment submission reviewed: {SubmissionId} by Reviewer {ReviewerId}, Status: {Status}", 
            submissionId, reviewerId, status);

        return true;
    }

    public async Task<IEnumerable<AssignmentSubmission>> GetPendingSubmissionsAsync()
    {
        return await _context.AssignmentSubmissions
            .Where(asub => asub.Status == SubmissionStatus.Pending)
            .Include(asub => asub.Assignment)
                .ThenInclude(a => a!.Lesson)
                    .ThenInclude(l => l!.Module)
                        .ThenInclude(m => m.Course)
            .Include(asub => asub.User)
            .OrderBy(asub => asub.SubmittedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AssignmentSubmission>> GetSubmissionsForReviewAsync(int reviewerId)
    {
        return await _context.AssignmentSubmissions
            .Where(asub => asub.ReviewedBy == reviewerId || asub.Status == SubmissionStatus.Pending)
            .Include(asub => asub.Assignment)
                .ThenInclude(a => a!.Lesson)
                    .ThenInclude(l => l!.Module)
                        .ThenInclude(m => m.Course)
            .Include(asub => asub.User)
            .OrderByDescending(asub => asub.SubmittedAt)
            .ToListAsync();
    }

    #endregion

    #region File Management

    public async Task<string> UploadSubmissionFileAsync(IFormFile file, int userId, int assignmentId)
    {
        // Create uploads directory if it doesn't exist
        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "assignments");
        Directory.CreateDirectory(uploadsPath);

        // Generate unique filename
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{assignmentId}_{userId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{fileExtension}";
        var filePath = Path.Combine(uploadsPath, uniqueFileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileUrl = $"/uploads/assignments/{uniqueFileName}";
        
        _logger.LogInformation("Assignment file uploaded: {FileUrl} for User {UserId}, Assignment {AssignmentId}", 
            fileUrl, userId, assignmentId);

        return fileUrl;
    }

    public Task<bool> DeleteSubmissionFileAsync(string fileUrl)
    {
        try
        {
            var filePath = Path.Combine(_environment.WebRootPath, fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Assignment file deleted: {FileUrl}", fileUrl);
                return Task.FromResult(true);
            }
            
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assignment file: {FileUrl}", fileUrl);
            return Task.FromResult(false);
        }
    }

    public async Task<FileDownloadInfo?> GetSubmissionFileAsync(string fileUrl)
    {
        try
        {
            var filePath = Path.Combine(_environment.WebRootPath, fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            
            if (!File.Exists(filePath))
            {
                return null;
            }

            var fileContent = await File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);
            var contentType = GetContentType(fileName);

            return new FileDownloadInfo
            {
                FileName = fileName,
                ContentType = contentType,
                FileContent = fileContent,
                FileSize = fileContent.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading assignment file: {FileUrl}", fileUrl);
            return null;
        }
    }

    #endregion

    #region Validation

    public async Task<bool> CanUserSubmitAssignmentAsync(int userId, int assignmentId)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Lesson)
            .FirstOrDefaultAsync(a => a.AssignmentID == assignmentId);

        if (assignment == null)
        {
            return false;
        }

        // Check lesson access if assignment is attached to a lesson
        if (assignment.LessonID.HasValue)
        {
            if (!await _prerequisiteService.CanAccessLessonAsync(userId, assignment.LessonID.Value))
            {
                return false;
            }
        }

        // Check if assignment is overdue
        if (assignment.DueDate.HasValue && DateTime.UtcNow > assignment.DueDate.Value)
        {
            return false;
        }

        return true;
    }

    public async Task<bool> IsFileTypeAllowedAsync(int assignmentId, string fileName)
    {
        var assignment = await _context.Assignments.FindAsync(assignmentId);
        if (assignment == null)
        {
            return false;
        }

        var fileExtension = Path.GetExtension(fileName).TrimStart('.').ToLower();
        return assignment.IsFileTypeAllowed(fileExtension);
    }

    public async Task<bool> IsFileSizeValidAsync(int assignmentId, long fileSize)
    {
        var assignment = await _context.Assignments.FindAsync(assignmentId);
        if (assignment == null)
        {
            return false;
        }

        return fileSize <= assignment.MaxFileSize;
    }

    #endregion

    #region Private Helper Methods

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }

    #endregion
}