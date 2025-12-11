using LMSPlatform.Models;

namespace LMSPlatform.Services;

public interface IPrerequisiteService
{
    // Lesson access control
    Task<bool> CanAccessLessonAsync(int userId, int lessonId);
    Task<LessonAccessResult> CheckLessonAccessAsync(int userId, int lessonId);
    Task<IEnumerable<Lesson>> GetAccessibleLessonsAsync(int userId, int moduleId);
    
    // Module access control
    Task<bool> CanAccessModuleAsync(int userId, int moduleId);
    Task<ModuleAccessResult> CheckModuleAccessAsync(int userId, int moduleId);
    Task<IEnumerable<Module>> GetAccessibleModulesAsync(int userId, int courseId);
    
    // Course access control
    Task<bool> CanAccessCourseAsync(int userId, int courseId);
    Task<CourseAccessResult> CheckCourseAccessAsync(int userId, int courseId);
    
    // Prerequisite validation
    Task<PrerequisiteValidationResult> ValidatePrerequisitesAsync(int userId, int lessonId);
    Task<IEnumerable<Lesson>> GetMissingPrerequisitesAsync(int userId, int lessonId);
    
    // Progress-based unlocking
    Task<Lesson?> GetNextUnlockedLessonAsync(int userId, int courseId);
    Task<bool> UnlockNextLessonAsync(int userId, int completedLessonId);
    
    // Quiz and assignment prerequisites
    Task<bool> CanTakeQuizAsync(int userId, int quizId);
    Task<bool> CanSubmitAssignmentAsync(int userId, int assignmentId);
    Task<bool> IsQuizRequiredForProgressionAsync(int quizId);
    
    // Enrollment and approval checks
    Task<bool> IsUserEnrolledAndApprovedAsync(int userId, int courseId);
    Task<UserEnrollmentStatus> GetUserEnrollmentStatusAsync(int userId, int courseId);
}

public class LessonAccessResult
{
    public bool CanAccess { get; set; }
    public string Reason { get; set; } = string.Empty;
    public IEnumerable<string> MissingPrerequisites { get; set; } = new List<string>();
    public bool RequiresEnrollment { get; set; }
    public bool RequiresPayment { get; set; }
    public bool RequiresPreviousCompletion { get; set; }
    public Lesson? BlockingLesson { get; set; }
}

public class ModuleAccessResult
{
    public bool CanAccess { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal CompletionRequiredPercentage { get; set; }
    public decimal CurrentCompletionPercentage { get; set; }
    public Module? PreviousModule { get; set; }
    public bool RequiresEnrollment { get; set; }
}

public class CourseAccessResult
{
    public bool CanAccess { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsEnrolled { get; set; }
    public bool IsApproved { get; set; }
    public bool PaymentRequired { get; set; }
    public UserEnrollmentStatus EnrollmentStatus { get; set; }
}

public class PrerequisiteValidationResult
{
    public bool IsValid { get; set; }
    public IEnumerable<string> Violations { get; set; } = new List<string>();
    public IEnumerable<Lesson> MissingLessons { get; set; } = new List<Lesson>();
    public IEnumerable<Quiz> RequiredQuizzes { get; set; } = new List<Quiz>();
    public bool CanBypass { get; set; } // For admin users
}

public enum UserEnrollmentStatus
{
    NotEnrolled,
    PendingPayment,
    Approved,
    Completed
}