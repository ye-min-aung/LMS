using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Services;

public class PrerequisiteService : IPrerequisiteService
{
    private readonly LMSDbContext _context;
    private readonly ILogger<PrerequisiteService> _logger;

    public PrerequisiteService(LMSDbContext context, ILogger<PrerequisiteService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Lesson Access Control

    public async Task<bool> CanAccessLessonAsync(int userId, int lessonId)
    {
        var result = await CheckLessonAccessAsync(userId, lessonId);
        return result.CanAccess;
    }

    public async Task<LessonAccessResult> CheckLessonAccessAsync(int userId, int lessonId)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Module)
                .ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(l => l.LessonID == lessonId);

        if (lesson == null)
        {
            return new LessonAccessResult
            {
                CanAccess = false,
                Reason = "Lesson not found"
            };
        }

        // Check if user is admin (admins can access everything)
        var user = await _context.Users.FindAsync(userId);
        if (user?.Role == UserRoles.Admin)
        {
            return new LessonAccessResult { CanAccess = true, Reason = "Admin access" };
        }

        // Check enrollment and approval
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == lesson.Module.CourseID);

        if (enrollment == null)
        {
            return new LessonAccessResult
            {
                CanAccess = false,
                Reason = "Not enrolled in course",
                RequiresEnrollment = true
            };
        }

        if (!enrollment.IsActive)
        {
            return new LessonAccessResult
            {
                CanAccess = false,
                Reason = "Payment approval required",
                RequiresPayment = true
            };
        }

        // Check module access
        var moduleAccess = await CheckModuleAccessAsync(userId, lesson.ModuleID);
        if (!moduleAccess.CanAccess)
        {
            return new LessonAccessResult
            {
                CanAccess = false,
                Reason = $"Module access denied: {moduleAccess.Reason}",
                RequiresPreviousCompletion = true
            };
        }

        // Check if this is the first lesson in the module
        if (lesson.LessonOrder == 1)
        {
            return new LessonAccessResult { CanAccess = true, Reason = "First lesson in module" };
        }

        // Check if previous lesson is completed
        var previousLesson = await _context.Lessons
            .Where(l => l.ModuleID == lesson.ModuleID && l.LessonOrder < lesson.LessonOrder)
            .OrderByDescending(l => l.LessonOrder)
            .FirstOrDefaultAsync();

        if (previousLesson == null)
        {
            return new LessonAccessResult { CanAccess = true, Reason = "No previous lesson required" };
        }

        var previousLessonProgress = await _context.LessonProgresses
            .FirstOrDefaultAsync(lp => lp.EnrollmentID == enrollment.EnrollmentID && 
                                      lp.LessonID == previousLesson.LessonID);

        if (previousLessonProgress?.IsCompleted != true)
        {
            return new LessonAccessResult
            {
                CanAccess = false,
                Reason = "Previous lesson not completed",
                RequiresPreviousCompletion = true,
                BlockingLesson = previousLesson,
                MissingPrerequisites = new[] { $"Complete lesson: {previousLesson.GetTitle()}" }
            };
        }

        // Check if previous lesson had a required quiz
        if (previousLesson.Quiz?.RequiredToUnlock == true)
        {
            var quizPassed = await _context.QuizAttempts
                .AnyAsync(qa => qa.UserID == userId && 
                               qa.QuizID == previousLesson.Quiz.QuizID && 
                               qa.Passed);

            if (!quizPassed)
            {
                return new LessonAccessResult
                {
                    CanAccess = false,
                    Reason = "Required quiz not passed",
                    RequiresPreviousCompletion = true,
                    MissingPrerequisites = new[] { $"Pass quiz in lesson: {previousLesson.GetTitle()}" }
                };
            }
        }

        return new LessonAccessResult { CanAccess = true, Reason = "All prerequisites met" };
    }

    public async Task<IEnumerable<Lesson>> GetAccessibleLessonsAsync(int userId, int moduleId)
    {
        var lessons = await _context.Lessons
            .Where(l => l.ModuleID == moduleId)
            .OrderBy(l => l.LessonOrder)
            .ToListAsync();

        var accessibleLessons = new List<Lesson>();

        foreach (var lesson in lessons)
        {
            if (await CanAccessLessonAsync(userId, lesson.LessonID))
            {
                accessibleLessons.Add(lesson);
            }
        }

        return accessibleLessons;
    }

    #endregion

    #region Module Access Control

    public async Task<bool> CanAccessModuleAsync(int userId, int moduleId)
    {
        var result = await CheckModuleAccessAsync(userId, moduleId);
        return result.CanAccess;
    }

    public async Task<ModuleAccessResult> CheckModuleAccessAsync(int userId, int moduleId)
    {
        var module = await _context.Modules
            .Include(m => m.Course)
                .ThenInclude(c => c.Modules.OrderBy(m => m.ModuleOrder))
                    .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(m => m.ModuleID == moduleId);

        if (module == null)
        {
            return new ModuleAccessResult
            {
                CanAccess = false,
                Reason = "Module not found"
            };
        }

        // Check if user is admin
        var user = await _context.Users.FindAsync(userId);
        if (user?.Role == UserRoles.Admin)
        {
            return new ModuleAccessResult { CanAccess = true, Reason = "Admin access" };
        }

        // Check enrollment
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == module.CourseID);

        if (enrollment == null)
        {
            return new ModuleAccessResult
            {
                CanAccess = false,
                Reason = "Not enrolled in course",
                RequiresEnrollment = true
            };
        }

        if (!enrollment.IsActive)
        {
            return new ModuleAccessResult
            {
                CanAccess = false,
                Reason = "Payment approval required"
            };
        }

        // First module is always accessible for enrolled students
        if (module.ModuleOrder == 1)
        {
            return new ModuleAccessResult { CanAccess = true, Reason = "First module in course" };
        }

        // Check if previous module is completed
        var previousModule = module.Course.Modules
            .Where(m => m.ModuleOrder < module.ModuleOrder)
            .OrderByDescending(m => m.ModuleOrder)
            .FirstOrDefault();

        if (previousModule == null)
        {
            return new ModuleAccessResult { CanAccess = true, Reason = "No previous module" };
        }

        // Calculate completion percentage of previous module
        var previousModuleLessons = previousModule.Lessons.ToList();
        if (!previousModuleLessons.Any())
        {
            return new ModuleAccessResult { CanAccess = true, Reason = "Previous module has no lessons" };
        }

        var completedLessonsCount = await _context.LessonProgresses
            .CountAsync(lp => lp.EnrollmentID == enrollment.EnrollmentID &&
                             previousModuleLessons.Select(l => l.LessonID).Contains(lp.LessonID) &&
                             lp.IsCompleted);

        var completionPercentage = (decimal)completedLessonsCount / previousModuleLessons.Count * 100;

        if (completionPercentage < 100)
        {
            return new ModuleAccessResult
            {
                CanAccess = false,
                Reason = "Previous module not completed",
                CompletionRequiredPercentage = 100,
                CurrentCompletionPercentage = completionPercentage,
                PreviousModule = previousModule
            };
        }

        return new ModuleAccessResult { CanAccess = true, Reason = "Previous module completed" };
    }

    public async Task<IEnumerable<Module>> GetAccessibleModulesAsync(int userId, int courseId)
    {
        var modules = await _context.Modules
            .Where(m => m.CourseID == courseId)
            .OrderBy(m => m.ModuleOrder)
            .ToListAsync();

        var accessibleModules = new List<Module>();

        foreach (var module in modules)
        {
            if (await CanAccessModuleAsync(userId, module.ModuleID))
            {
                accessibleModules.Add(module);
            }
        }

        return accessibleModules;
    }

    #endregion

    #region Course Access Control

    public async Task<bool> CanAccessCourseAsync(int userId, int courseId)
    {
        var result = await CheckCourseAccessAsync(userId, courseId);
        return result.CanAccess;
    }

    public async Task<CourseAccessResult> CheckCourseAccessAsync(int userId, int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null)
        {
            return new CourseAccessResult
            {
                CanAccess = false,
                Reason = "Course not found"
            };
        }

        // Check if user is admin
        var user = await _context.Users.FindAsync(userId);
        if (user?.Role == UserRoles.Admin)
        {
            return new CourseAccessResult 
            { 
                CanAccess = true, 
                Reason = "Admin access",
                IsEnrolled = true,
                IsApproved = true,
                EnrollmentStatus = UserEnrollmentStatus.Approved
            };
        }

        // Check enrollment
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId);

        if (enrollment == null)
        {
            return new CourseAccessResult
            {
                CanAccess = false,
                Reason = "Not enrolled in course",
                IsEnrolled = false,
                PaymentRequired = true,
                EnrollmentStatus = UserEnrollmentStatus.NotEnrolled
            };
        }

        var enrollmentStatus = enrollment.Status switch
        {
            Models.EnrollmentStatus.PendingPayment => UserEnrollmentStatus.PendingPayment,
            Models.EnrollmentStatus.Approved => UserEnrollmentStatus.Approved,
            Models.EnrollmentStatus.Completed => UserEnrollmentStatus.Completed,
            _ => UserEnrollmentStatus.NotEnrolled
        };

        if (!enrollment.IsActive)
        {
            return new CourseAccessResult
            {
                CanAccess = false,
                Reason = "Payment approval required",
                IsEnrolled = true,
                IsApproved = false,
                PaymentRequired = true,
                EnrollmentStatus = enrollmentStatus
            };
        }

        return new CourseAccessResult
        {
            CanAccess = true,
            Reason = "Enrolled and approved",
            IsEnrolled = true,
            IsApproved = true,
            EnrollmentStatus = enrollmentStatus
        };
    }

    #endregion

    #region Prerequisite Validation

    public async Task<PrerequisiteValidationResult> ValidatePrerequisitesAsync(int userId, int lessonId)
    {
        var user = await _context.Users.FindAsync(userId);
        var canBypass = user?.Role == UserRoles.Admin;

        var accessResult = await CheckLessonAccessAsync(userId, lessonId);
        
        if (accessResult.CanAccess || canBypass)
        {
            return new PrerequisiteValidationResult
            {
                IsValid = true,
                CanBypass = canBypass
            };
        }

        var missingLessons = await GetMissingPrerequisitesAsync(userId, lessonId);
        var violations = accessResult.MissingPrerequisites.ToList();

        return new PrerequisiteValidationResult
        {
            IsValid = false,
            Violations = violations,
            MissingLessons = missingLessons,
            CanBypass = canBypass
        };
    }

    public async Task<IEnumerable<Lesson>> GetMissingPrerequisitesAsync(int userId, int lessonId)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Module)
            .FirstOrDefaultAsync(l => l.LessonID == lessonId);

        if (lesson == null)
        {
            return Enumerable.Empty<Lesson>();
        }

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == lesson.Module.CourseID);

        if (enrollment == null)
        {
            return Enumerable.Empty<Lesson>();
        }

        var previousLessons = await _context.Lessons
            .Where(l => l.ModuleID == lesson.ModuleID && l.LessonOrder < lesson.LessonOrder)
            .OrderBy(l => l.LessonOrder)
            .ToListAsync();

        var missingLessons = new List<Lesson>();

        foreach (var prevLesson in previousLessons)
        {
            var progress = await _context.LessonProgresses
                .FirstOrDefaultAsync(lp => lp.EnrollmentID == enrollment.EnrollmentID && 
                                          lp.LessonID == prevLesson.LessonID);

            if (progress?.IsCompleted != true)
            {
                missingLessons.Add(prevLesson);
            }
        }

        return missingLessons;
    }

    #endregion

    #region Progress-based Unlocking

    public async Task<Lesson?> GetNextUnlockedLessonAsync(int userId, int courseId)
    {
        var modules = await _context.Modules
            .Where(m => m.CourseID == courseId)
            .Include(m => m.Lessons)
            .OrderBy(m => m.ModuleOrder)
            .ToListAsync();

        foreach (var module in modules)
        {
            if (!await CanAccessModuleAsync(userId, module.ModuleID))
            {
                continue;
            }

            var lessons = module.Lessons.OrderBy(l => l.LessonOrder).ToList();
            foreach (var lesson in lessons)
            {
                if (await CanAccessLessonAsync(userId, lesson.LessonID))
                {
                    // Check if this lesson is not completed yet
                    var enrollment = await _context.Enrollments
                        .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId);

                    if (enrollment != null)
                    {
                        var progress = await _context.LessonProgresses
                            .FirstOrDefaultAsync(lp => lp.EnrollmentID == enrollment.EnrollmentID && 
                                                      lp.LessonID == lesson.LessonID);

                        if (progress?.IsCompleted != true)
                        {
                            return lesson;
                        }
                    }
                }
                else
                {
                    // This is the first inaccessible lesson, so return null
                    return null;
                }
            }
        }

        return null; // All lessons completed or no accessible lessons
    }

    public async Task<bool> UnlockNextLessonAsync(int userId, int completedLessonId)
    {
        var completedLesson = await _context.Lessons
            .Include(l => l.Module)
            .FirstOrDefaultAsync(l => l.LessonID == completedLessonId);

        if (completedLesson == null)
        {
            return false;
        }

        // Find next lesson in the same module
        var nextLesson = await _context.Lessons
            .Where(l => l.ModuleID == completedLesson.ModuleID && 
                       l.LessonOrder > completedLesson.LessonOrder)
            .OrderBy(l => l.LessonOrder)
            .FirstOrDefaultAsync();

        if (nextLesson != null)
        {
            return await CanAccessLessonAsync(userId, nextLesson.LessonID);
        }

        // Check if next module should be unlocked
        var nextModule = await _context.Modules
            .Where(m => m.CourseID == completedLesson.Module.CourseID && 
                       m.ModuleOrder > completedLesson.Module.ModuleOrder)
            .OrderBy(m => m.ModuleOrder)
            .FirstOrDefaultAsync();

        if (nextModule != null)
        {
            return await CanAccessModuleAsync(userId, nextModule.ModuleID);
        }

        return false; // No next lesson or module
    }

    #endregion

    #region Quiz and Assignment Prerequisites

    public async Task<bool> CanTakeQuizAsync(int userId, int quizId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Lesson)
            .FirstOrDefaultAsync(q => q.QuizID == quizId);

        if (quiz?.Lesson == null)
        {
            return false;
        }

        return await CanAccessLessonAsync(userId, quiz.Lesson.LessonID);
    }

    public async Task<bool> CanSubmitAssignmentAsync(int userId, int assignmentId)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Lesson)
            .FirstOrDefaultAsync(a => a.AssignmentID == assignmentId);

        if (assignment?.Lesson == null)
        {
            return false;
        }

        return await CanAccessLessonAsync(userId, assignment.Lesson.LessonID);
    }

    public async Task<bool> IsQuizRequiredForProgressionAsync(int quizId)
    {
        var quiz = await _context.Quizzes.FindAsync(quizId);
        return quiz?.RequiredToUnlock == true;
    }

    #endregion

    #region Enrollment and Approval Checks

    public async Task<bool> IsUserEnrolledAndApprovedAsync(int userId, int courseId)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId);

        return enrollment?.IsActive == true;
    }

    public async Task<UserEnrollmentStatus> GetUserEnrollmentStatusAsync(int userId, int courseId)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId);

        if (enrollment == null)
        {
            return UserEnrollmentStatus.NotEnrolled;
        }

        return enrollment.Status switch
        {
            Models.EnrollmentStatus.PendingPayment => UserEnrollmentStatus.PendingPayment,
            Models.EnrollmentStatus.Approved => UserEnrollmentStatus.Approved,
            Models.EnrollmentStatus.Completed => UserEnrollmentStatus.Completed,
            _ => UserEnrollmentStatus.NotEnrolled
        };
    }

    #endregion
}