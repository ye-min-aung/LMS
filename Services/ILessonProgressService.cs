using LMSPlatform.Models;

namespace LMSPlatform.Services;

public interface ILessonProgressService
{
    // Progress tracking
    Task<LessonProgress> UpdateLessonProgressAsync(int enrollmentId, int lessonId, bool completed, int? videoTimestamp = null);
    Task<LessonProgress?> GetLessonProgressAsync(int enrollmentId, int lessonId);
    Task<IEnumerable<LessonProgress>> GetEnrollmentProgressAsync(int enrollmentId);
    
    // Video progress
    Task<bool> SaveVideoTimestampAsync(int enrollmentId, int lessonId, int timestamp);
    Task<int?> GetVideoTimestampAsync(int enrollmentId, int lessonId);
    Task<bool> MarkVideoLessonCompleteAsync(int enrollmentId, int lessonId);
    
    // Progress calculations
    Task<decimal> CalculateCourseProgressAsync(int enrollmentId);
    Task<decimal> CalculateModuleProgressAsync(int enrollmentId, int moduleId);
    Task<CourseProgressSummary> GetCourseProgressSummaryAsync(int enrollmentId);
    
    // Resume functionality
    Task<Lesson?> GetLastAccessedLessonAsync(int enrollmentId);
    Task<LessonResumeInfo?> GetLessonResumeInfoAsync(int enrollmentId, int lessonId);
    
    // Completion tracking
    Task<bool> IsLessonCompletedAsync(int enrollmentId, int lessonId);
    Task<bool> IsModuleCompletedAsync(int enrollmentId, int moduleId);
    Task<bool> IsCourseCompletedAsync(int enrollmentId);
    Task<DateTime?> GetCourseCompletionDateAsync(int enrollmentId);
}

public class CourseProgressSummary
{
    public int EnrollmentId { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public decimal ProgressPercentage { get; set; }
    public int TotalModules { get; set; }
    public int CompletedModules { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public Lesson? LastAccessedLesson { get; set; }
    public TimeSpan TotalTimeSpent { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletionDate { get; set; }
}

public class LessonResumeInfo
{
    public Lesson Lesson { get; set; } = null!;
    public int? VideoTimestamp { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public bool CanResume { get; set; }
}