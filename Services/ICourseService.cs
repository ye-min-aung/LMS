using LMSPlatform.Models;

namespace LMSPlatform.Services;

public interface ICourseService
{
    // Course management
    Task<IEnumerable<Course>> GetCoursesAsync(string language = "en");
    Task<Course?> GetCourseByIdAsync(int courseId, string language = "en");
    Task<Course> CreateCourseAsync(CreateCourseModel model);
    Task<bool> UpdateCourseAsync(int courseId, UpdateCourseModel model);
    Task<bool> DeleteCourseAsync(int courseId);
    
    // Module management
    Task<Module?> GetModuleByIdAsync(int moduleId);
    Task<Module> CreateModuleAsync(CreateModuleModel model);
    Task<bool> UpdateModuleAsync(int moduleId, UpdateModuleModel model);
    Task<bool> DeleteModuleAsync(int moduleId);
    Task<IEnumerable<Module>> GetCourseModulesAsync(int courseId);
    
    // Lesson management
    Task<Lesson?> GetLessonByIdAsync(int lessonId);
    Task<Lesson> CreateLessonAsync(CreateLessonModel model);
    Task<bool> UpdateLessonAsync(int lessonId, UpdateLessonModel model);
    Task<bool> DeleteLessonAsync(int lessonId);
    Task<IEnumerable<Lesson>> GetModuleLessonsAsync(int moduleId);
    
    // Enrollment management
    Task<bool> EnrollStudentAsync(int userId, int courseId);
    Task<Enrollment?> GetEnrollmentAsync(int userId, int courseId);
    Task<IEnumerable<Enrollment>> GetUserEnrollmentsAsync(int userId);
    Task<bool> IsUserEnrolledAsync(int userId, int courseId);
    
    // Course structure and navigation
    Task<CourseStructureModel> GetCourseStructureAsync(int courseId, int? userId = null);
    Task<Lesson?> GetNextLessonAsync(int currentLessonId, int userId);
    Task<Lesson?> GetPreviousLessonAsync(int currentLessonId, int userId);
}

// DTOs for course operations
public class CreateCourseModel
{
    public string Title_EN { get; set; } = string.Empty;
    public string Title_MM { get; set; } = string.Empty;
    public string? Description_EN { get; set; }
    public string? Description_MM { get; set; }
    public decimal Price { get; set; }
}

public class UpdateCourseModel
{
    public string Title_EN { get; set; } = string.Empty;
    public string Title_MM { get; set; } = string.Empty;
    public string? Description_EN { get; set; }
    public string? Description_MM { get; set; }
    public decimal Price { get; set; }
}

public class CreateModuleModel
{
    public int CourseID { get; set; }
    public string Title_EN { get; set; } = string.Empty;
    public string Title_MM { get; set; } = string.Empty;
    public int ModuleOrder { get; set; }
}

public class UpdateModuleModel
{
    public string Title_EN { get; set; } = string.Empty;
    public string Title_MM { get; set; } = string.Empty;
    public int ModuleOrder { get; set; }
}

public class CreateLessonModel
{
    public int ModuleID { get; set; }
    public string Title_EN { get; set; } = string.Empty;
    public string Title_MM { get; set; } = string.Empty;
    public int LessonOrder { get; set; }
    public string LessonType { get; set; } = string.Empty;
    public string? ContentURL { get; set; }
    public string? Content_EN { get; set; }
    public string? Content_MM { get; set; }
}

public class UpdateLessonModel
{
    public string Title_EN { get; set; } = string.Empty;
    public string Title_MM { get; set; } = string.Empty;
    public int LessonOrder { get; set; }
    public string LessonType { get; set; } = string.Empty;
    public string? ContentURL { get; set; }
    public string? Content_EN { get; set; }
    public string? Content_MM { get; set; }
}

public class CourseStructureModel
{
    public Course Course { get; set; } = null!;
    public List<ModuleStructureModel> Modules { get; set; } = new();
    public bool IsEnrolled { get; set; }
    public bool IsApproved { get; set; }
    public decimal ProgressPercentage { get; set; }
}

public class ModuleStructureModel
{
    public Module Module { get; set; } = null!;
    public List<LessonStructureModel> Lessons { get; set; } = new();
    public bool IsUnlocked { get; set; }
    public decimal ProgressPercentage { get; set; }
}

public class LessonStructureModel
{
    public Lesson Lesson { get; set; } = null!;
    public bool IsCompleted { get; set; }
    public bool IsUnlocked { get; set; }
    public int? VideoTimestamp { get; set; }
    public bool HasQuiz { get; set; }
    public bool HasAssignment { get; set; }
    public bool QuizPassed { get; set; }
    public bool AssignmentSubmitted { get; set; }
}