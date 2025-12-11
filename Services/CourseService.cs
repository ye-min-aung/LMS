using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Services;

public class CourseService : ICourseService
{
    private readonly LMSDbContext _context;
    private readonly ILogger<CourseService> _logger;

    public CourseService(LMSDbContext context, ILogger<CourseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Course Management

    public async Task<IEnumerable<Course>> GetCoursesAsync(string language = "en")
    {
        return await _context.Courses
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Course?> GetCourseByIdAsync(int courseId, string language = "en")
    {
        return await _context.Courses
            .Include(c => c.Modules.OrderBy(m => m.ModuleOrder))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.LessonOrder))
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.CourseID == courseId);
    }

    public async Task<Course> CreateCourseAsync(CreateCourseModel model)
    {
        var course = new Course
        {
            Title_EN = model.Title_EN,
            Title_MM = model.Title_MM,
            Description_EN = model.Description_EN,
            Description_MM = model.Description_MM,
            Price = model.Price
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Course created: {CourseId} - {Title}", course.CourseID, course.Title_EN);
        return course;
    }

    public async Task<bool> UpdateCourseAsync(int courseId, UpdateCourseModel model)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null)
        {
            return false;
        }

        course.Title_EN = model.Title_EN;
        course.Title_MM = model.Title_MM;
        course.Description_EN = model.Description_EN;
        course.Description_MM = model.Description_MM;
        course.Price = model.Price;
        course.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Course updated: {CourseId}", courseId);
        return true;
    }

    public async Task<bool> DeleteCourseAsync(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null)
        {
            return false;
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Course deleted: {CourseId}", courseId);
        return true;
    }

    #endregion

    #region Module Management

    public async Task<Module?> GetModuleByIdAsync(int moduleId)
    {
        return await _context.Modules
            .Include(m => m.Course)
            .Include(m => m.Lessons.OrderBy(l => l.LessonOrder))
            .FirstOrDefaultAsync(m => m.ModuleID == moduleId);
    }

    public async Task<Module> CreateModuleAsync(CreateModuleModel model)
    {
        var module = new Module
        {
            CourseID = model.CourseID,
            Title_EN = model.Title_EN,
            Title_MM = model.Title_MM,
            ModuleOrder = model.ModuleOrder
        };

        _context.Modules.Add(module);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Module created: {ModuleId} - {Title}", module.ModuleID, module.Title_EN);
        return module;
    }

    public async Task<bool> UpdateModuleAsync(int moduleId, UpdateModuleModel model)
    {
        var module = await _context.Modules.FindAsync(moduleId);
        if (module == null)
        {
            return false;
        }

        module.Title_EN = model.Title_EN;
        module.Title_MM = model.Title_MM;
        module.ModuleOrder = model.ModuleOrder;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Module updated: {ModuleId}", moduleId);
        return true;
    }

    public async Task<bool> DeleteModuleAsync(int moduleId)
    {
        var module = await _context.Modules.FindAsync(moduleId);
        if (module == null)
        {
            return false;
        }

        _context.Modules.Remove(module);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Module deleted: {ModuleId}", moduleId);
        return true;
    }

    public async Task<IEnumerable<Module>> GetCourseModulesAsync(int courseId)
    {
        return await _context.Modules
            .Where(m => m.CourseID == courseId)
            .Include(m => m.Lessons.OrderBy(l => l.LessonOrder))
            .OrderBy(m => m.ModuleOrder)
            .ToListAsync();
    }

    #endregion

    #region Lesson Management

    public async Task<Lesson?> GetLessonByIdAsync(int lessonId)
    {
        return await _context.Lessons
            .Include(l => l.Module)
                .ThenInclude(m => m.Course)
            .Include(l => l.Quiz)
                .ThenInclude(q => q!.Questions)
                    .ThenInclude(q => q.AnswerChoices)
            .Include(l => l.Assignment)
            .FirstOrDefaultAsync(l => l.LessonID == lessonId);
    }

    public async Task<Lesson> CreateLessonAsync(CreateLessonModel model)
    {
        var lesson = new Lesson
        {
            ModuleID = model.ModuleID,
            Title_EN = model.Title_EN,
            Title_MM = model.Title_MM,
            LessonOrder = model.LessonOrder,
            LessonType = model.LessonType,
            ContentURL = model.ContentURL,
            Content_EN = model.Content_EN,
            Content_MM = model.Content_MM
        };

        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Lesson created: {LessonId} - {Title}", lesson.LessonID, lesson.Title_EN);
        return lesson;
    }

    public async Task<bool> UpdateLessonAsync(int lessonId, UpdateLessonModel model)
    {
        var lesson = await _context.Lessons.FindAsync(lessonId);
        if (lesson == null)
        {
            return false;
        }

        lesson.Title_EN = model.Title_EN;
        lesson.Title_MM = model.Title_MM;
        lesson.LessonOrder = model.LessonOrder;
        lesson.LessonType = model.LessonType;
        lesson.ContentURL = model.ContentURL;
        lesson.Content_EN = model.Content_EN;
        lesson.Content_MM = model.Content_MM;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Lesson updated: {LessonId}", lessonId);
        return true;
    }

    public async Task<bool> DeleteLessonAsync(int lessonId)
    {
        var lesson = await _context.Lessons.FindAsync(lessonId);
        if (lesson == null)
        {
            return false;
        }

        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Lesson deleted: {LessonId}", lessonId);
        return true;
    }

    public async Task<IEnumerable<Lesson>> GetModuleLessonsAsync(int moduleId)
    {
        return await _context.Lessons
            .Where(l => l.ModuleID == moduleId)
            .Include(l => l.Quiz)
            .Include(l => l.Assignment)
            .OrderBy(l => l.LessonOrder)
            .ToListAsync();
    }

    #endregion

    #region Enrollment Management

    public async Task<bool> EnrollStudentAsync(int userId, int courseId)
    {
        // Check if already enrolled
        var existingEnrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId);

        if (existingEnrollment != null)
        {
            return false; // Already enrolled
        }

        var enrollment = new Enrollment
        {
            UserID = userId,
            CourseID = courseId,
            Status = Models.EnrollmentStatus.PendingPayment
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Student enrolled: UserId {UserId} in Course {CourseId}", userId, courseId);
        return true;
    }

    public async Task<Enrollment?> GetEnrollmentAsync(int userId, int courseId)
    {
        return await _context.Enrollments
            .Include(e => e.Course)
            .Include(e => e.User)
            .Include(e => e.LessonProgresses)
                .ThenInclude(lp => lp.Lesson)
            .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId);
    }

    public async Task<IEnumerable<Enrollment>> GetUserEnrollmentsAsync(int userId)
    {
        return await _context.Enrollments
            .Where(e => e.UserID == userId)
            .Include(e => e.Course)
                .ThenInclude(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
            .Include(e => e.LessonProgresses)
            .OrderByDescending(e => e.EnrollmentDate)
            .ToListAsync();
    }

    public async Task<bool> IsUserEnrolledAsync(int userId, int courseId)
    {
        return await _context.Enrollments
            .AnyAsync(e => e.UserID == userId && e.CourseID == courseId);
    }

    #endregion

    #region Course Structure and Navigation

    public async Task<CourseStructureModel> GetCourseStructureAsync(int courseId, int? userId = null)
    {
        var course = await GetCourseByIdAsync(courseId);
        if (course == null)
        {
            throw new ArgumentException("Course not found", nameof(courseId));
        }

        var enrollment = userId.HasValue 
            ? await GetEnrollmentAsync(userId.Value, courseId)
            : null;

        var structure = new CourseStructureModel
        {
            Course = course,
            IsEnrolled = enrollment != null,
            IsApproved = enrollment?.IsActive == true,
            ProgressPercentage = enrollment?.GetProgressPercentage() ?? 0
        };

        foreach (var module in course.Modules)
        {
            var moduleStructure = new ModuleStructureModel
            {
                Module = module,
                IsUnlocked = await IsModuleUnlockedAsync(module.ModuleID, userId)
            };

            foreach (var lesson in module.Lessons)
            {
                var lessonProgress = enrollment?.LessonProgresses
                    .FirstOrDefault(lp => lp.LessonID == lesson.LessonID);

                var lessonStructure = new LessonStructureModel
                {
                    Lesson = lesson,
                    IsCompleted = lessonProgress?.IsCompleted ?? false,
                    IsUnlocked = await IsLessonUnlockedAsync(lesson.LessonID, userId),
                    VideoTimestamp = lessonProgress?.VideoTimestamp,
                    HasQuiz = lesson.Quiz != null,
                    HasAssignment = lesson.Assignment != null
                };

                // Check quiz and assignment status if user is enrolled
                if (userId.HasValue && enrollment != null)
                {
                    if (lesson.Quiz != null)
                    {
                        lessonStructure.QuizPassed = await _context.QuizAttempts
                            .AnyAsync(qa => qa.UserID == userId.Value && 
                                          qa.QuizID == lesson.Quiz.QuizID && 
                                          qa.Passed);
                    }

                    if (lesson.Assignment != null)
                    {
                        lessonStructure.AssignmentSubmitted = await _context.AssignmentSubmissions
                            .AnyAsync(asub => asub.UserID == userId.Value && 
                                            asub.AssignmentID == lesson.Assignment.AssignmentID);
                    }
                }

                moduleStructure.Lessons.Add(lessonStructure);
            }

            // Calculate module progress
            if (moduleStructure.Lessons.Any())
            {
                var completedLessons = moduleStructure.Lessons.Count(l => l.IsCompleted);
                moduleStructure.ProgressPercentage = (decimal)completedLessons / moduleStructure.Lessons.Count * 100;
            }

            structure.Modules.Add(moduleStructure);
        }

        return structure;
    }

    public async Task<Lesson?> GetNextLessonAsync(int currentLessonId, int userId)
    {
        var currentLesson = await _context.Lessons
            .Include(l => l.Module)
                .ThenInclude(m => m.Course)
                    .ThenInclude(c => c.Modules)
                        .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(l => l.LessonID == currentLessonId);

        if (currentLesson == null)
        {
            return null;
        }

        // Try to find next lesson in same module
        var nextLessonInModule = currentLesson.Module.Lessons
            .Where(l => l.LessonOrder > currentLesson.LessonOrder)
            .OrderBy(l => l.LessonOrder)
            .FirstOrDefault();

        if (nextLessonInModule != null && await IsLessonUnlockedAsync(nextLessonInModule.LessonID, userId))
        {
            return nextLessonInModule;
        }

        // Try to find first lesson in next module
        var nextModule = currentLesson.Module.Course.Modules
            .Where(m => m.ModuleOrder > currentLesson.Module.ModuleOrder)
            .OrderBy(m => m.ModuleOrder)
            .FirstOrDefault();

        if (nextModule != null)
        {
            var firstLessonInNextModule = nextModule.Lessons
                .OrderBy(l => l.LessonOrder)
                .FirstOrDefault();

            if (firstLessonInNextModule != null && await IsLessonUnlockedAsync(firstLessonInNextModule.LessonID, userId))
            {
                return firstLessonInNextModule;
            }
        }

        return null;
    }

    public async Task<Lesson?> GetPreviousLessonAsync(int currentLessonId, int userId)
    {
        var currentLesson = await _context.Lessons
            .Include(l => l.Module)
                .ThenInclude(m => m.Course)
                    .ThenInclude(c => c.Modules)
                        .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(l => l.LessonID == currentLessonId);

        if (currentLesson == null)
        {
            return null;
        }

        // Try to find previous lesson in same module
        var previousLessonInModule = currentLesson.Module.Lessons
            .Where(l => l.LessonOrder < currentLesson.LessonOrder)
            .OrderByDescending(l => l.LessonOrder)
            .FirstOrDefault();

        if (previousLessonInModule != null)
        {
            return previousLessonInModule;
        }

        // Try to find last lesson in previous module
        var previousModule = currentLesson.Module.Course.Modules
            .Where(m => m.ModuleOrder < currentLesson.Module.ModuleOrder)
            .OrderByDescending(m => m.ModuleOrder)
            .FirstOrDefault();

        if (previousModule != null)
        {
            return previousModule.Lessons
                .OrderByDescending(l => l.LessonOrder)
                .FirstOrDefault();
        }

        return null;
    }

    #endregion

    #region Private Helper Methods

    private async Task<bool> IsModuleUnlockedAsync(int moduleId, int? userId)
    {
        if (!userId.HasValue)
        {
            return false; // Guests can't access modules
        }

        var module = await _context.Modules
            .Include(m => m.Course)
                .ThenInclude(c => c.Modules.OrderBy(m => m.ModuleOrder))
                    .ThenInclude(m => m.Lessons.OrderBy(l => l.LessonOrder))
            .FirstOrDefaultAsync(m => m.ModuleID == moduleId);

        if (module == null)
        {
            return false;
        }

        // Check if user is enrolled and approved
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == userId.Value && e.CourseID == module.CourseID);

        if (enrollment == null || !enrollment.IsActive)
        {
            return false;
        }

        // First module is always unlocked for enrolled students
        if (module.ModuleOrder == 1)
        {
            return true;
        }

        // Check if previous module is completed
        var previousModule = module.Course.Modules
            .Where(m => m.ModuleOrder < module.ModuleOrder)
            .OrderByDescending(m => m.ModuleOrder)
            .FirstOrDefault();

        if (previousModule == null)
        {
            return true;
        }

        // Check if all lessons in previous module are completed
        var previousModuleLessons = previousModule.Lessons.ToList();
        if (!previousModuleLessons.Any())
        {
            return true;
        }

        var completedLessonsCount = await _context.LessonProgresses
            .CountAsync(lp => lp.EnrollmentID == enrollment.EnrollmentID &&
                             previousModuleLessons.Select(l => l.LessonID).Contains(lp.LessonID) &&
                             lp.IsCompleted);

        return completedLessonsCount == previousModuleLessons.Count;
    }

    private async Task<bool> IsLessonUnlockedAsync(int lessonId, int? userId)
    {
        if (!userId.HasValue)
        {
            return false; // Guests can't access lessons
        }

        var lesson = await _context.Lessons
            .Include(l => l.Module)
                .ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(l => l.LessonID == lessonId);

        if (lesson == null)
        {
            return false;
        }

        // Check if user is enrolled and approved
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == userId.Value && e.CourseID == lesson.Module.CourseID);

        if (enrollment == null || !enrollment.IsActive)
        {
            return false;
        }

        // Check if module is unlocked
        if (!await IsModuleUnlockedAsync(lesson.ModuleID, userId))
        {
            return false;
        }

        // First lesson in module is always unlocked if module is unlocked
        if (lesson.LessonOrder == 1)
        {
            return true;
        }

        // Check if previous lesson is completed
        var previousLesson = await _context.Lessons
            .Where(l => l.ModuleID == lesson.ModuleID && l.LessonOrder < lesson.LessonOrder)
            .OrderByDescending(l => l.LessonOrder)
            .FirstOrDefaultAsync();

        if (previousLesson == null)
        {
            return true;
        }

        var previousLessonProgress = await _context.LessonProgresses
            .FirstOrDefaultAsync(lp => lp.EnrollmentID == enrollment.EnrollmentID &&
                                      lp.LessonID == previousLesson.LessonID);

        return previousLessonProgress?.IsCompleted == true;
    }

    #endregion
}