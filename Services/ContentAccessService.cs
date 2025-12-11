using LMSPlatform.Models;

namespace LMSPlatform.Services;

/// <summary>
/// Service for handling content access validation and messaging
/// </summary>
public interface IContentAccessService
{
    Task<ContentAccessResult> CheckCourseAccessAsync(int userId, int courseId);
    Task<ContentAccessResult> CheckLessonAccessAsync(int userId, int lessonId);
    Task<ContentAccessResult> CheckQuizAccessAsync(int userId, int quizId);
    Task<ContentAccessResult> CheckAssignmentAccessAsync(int userId, int assignmentId);
    string GetAccessDeniedMessage(ContentAccessResult result);
}

public class ContentAccessService : IContentAccessService
{
    private readonly IPrerequisiteService _prerequisiteService;
    private readonly ICourseService _courseService;
    private readonly ILogger<ContentAccessService> _logger;

    public ContentAccessService(
        IPrerequisiteService prerequisiteService,
        ICourseService courseService,
        ILogger<ContentAccessService> logger)
    {
        _prerequisiteService = prerequisiteService;
        _courseService = courseService;
        _logger = logger;
    }

    public async Task<ContentAccessResult> CheckCourseAccessAsync(int userId, int courseId)
    {
        var accessResult = await _prerequisiteService.CheckCourseAccessAsync(userId, courseId);
        
        return new ContentAccessResult
        {
            HasAccess = accessResult.CanAccess,
            Reason = accessResult.Reason,
            RequiresEnrollment = !accessResult.IsEnrolled,
            RequiresPayment = accessResult.PaymentRequired,
            RequiresApproval = accessResult.IsEnrolled && !accessResult.IsApproved
        };
    }

    public async Task<ContentAccessResult> CheckLessonAccessAsync(int userId, int lessonId)
    {
        var accessResult = await _prerequisiteService.CheckLessonAccessAsync(userId, lessonId);
        
        return new ContentAccessResult
        {
            HasAccess = accessResult.CanAccess,
            Reason = accessResult.Reason,
            RequiresEnrollment = accessResult.RequiresEnrollment,
            RequiresPayment = accessResult.RequiresPayment,
            RequiresPreviousCompletion = accessResult.RequiresPreviousCompletion,
            BlockingLessonTitle = accessResult.BlockingLesson?.GetTitle(),
            MissingPrerequisites = accessResult.MissingPrerequisites.ToList()
        };
    }

    public async Task<ContentAccessResult> CheckQuizAccessAsync(int userId, int quizId)
    {
        var canAccess = await _prerequisiteService.CanTakeQuizAsync(userId, quizId);
        
        return new ContentAccessResult
        {
            HasAccess = canAccess,
            Reason = canAccess ? "Access granted" : "Quiz access denied - complete the lesson first"
        };
    }

    public async Task<ContentAccessResult> CheckAssignmentAccessAsync(int userId, int assignmentId)
    {
        var canAccess = await _prerequisiteService.CanSubmitAssignmentAsync(userId, assignmentId);
        
        return new ContentAccessResult
        {
            HasAccess = canAccess,
            Reason = canAccess ? "Access granted" : "Assignment access denied - complete the lesson first"
        };
    }

    public string GetAccessDeniedMessage(ContentAccessResult result)
    {
        if (result.HasAccess)
            return string.Empty;

        if (result.RequiresEnrollment)
            return "You need to enroll in this course to access this content.";

        if (result.RequiresPayment)
            return "Payment is required to access this content. Please complete your enrollment.";

        if (result.RequiresApproval)
            return "Your enrollment is pending approval. Please wait for admin confirmation.";

        if (result.RequiresPreviousCompletion)
        {
            if (!string.IsNullOrEmpty(result.BlockingLessonTitle))
                return $"Please complete the lesson \"{result.BlockingLessonTitle}\" before accessing this content.";
            
            if (result.MissingPrerequisites.Any())
                return $"Please complete the following prerequisites: {string.Join(", ", result.MissingPrerequisites)}";
            
            return "Please complete the previous lessons before accessing this content.";
        }

        return result.Reason ?? "Access denied.";
    }
}

public class ContentAccessResult
{
    public bool HasAccess { get; set; }
    public string? Reason { get; set; }
    public bool RequiresEnrollment { get; set; }
    public bool RequiresPayment { get; set; }
    public bool RequiresApproval { get; set; }
    public bool RequiresPreviousCompletion { get; set; }
    public string? BlockingLessonTitle { get; set; }
    public List<string> MissingPrerequisites { get; set; } = new();
}
