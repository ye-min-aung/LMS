using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Services;

public class LessonProgressService : ILessonProgressService
{
    private readonly LMSDbContext _context;
    private readonly ILogger<LessonProgressService> _logger;
    private readonly Lazy<ICertificateService> _certificateService;

    public LessonProgressService(
        LMSDbContext context, 
        ILogger<LessonProgressService> logger,
        Lazy<ICertificateService> certificateService)
    {
        _context = context;
        _logger = logger;
        _certificateService = certificateService;
    }

    #region Progress Tracking

    public async Task<LessonProgress> UpdateLessonProgressAsync(int enrollmentId, int lessonId, bool completed, int? videoTimestamp = null)
    {
        var progress = await _context.LessonProgresses
            .FirstOrDefaultAsync(lp => lp.EnrollmentID == enrollmentId && lp.LessonID == lessonId);

        if (progress == null)
        {
            progress = new LessonProgress
            {
                EnrollmentID = enrollmentId,
                LessonID = lessonId,
                IsCompleted = completed,
                VideoTimestamp = videoTimestamp
            };

            if (completed)
            {
                progress.MarkCompleted();
            }
            else
            {
                progress.UpdateProgress(videoTimestamp);
            }

            _context.LessonProgresses.Add(progress);
        }
        else
        {
            if (completed && !progress.IsCompleted)
            {
                progress.MarkCompleted();
            }
            else
            {
                progress.UpdateProgress(videoTimestamp);
            }
        }

        await _context.SaveChangesAsync();

        // Check if course is now completed
        if (completed)
        {
            await CheckAndUpdateCourseCompletionAsync(enrollmentId);
        }

        _logger.LogInformation("Lesson progress updated: Enrollment {EnrollmentId}, Lesson {LessonId}, Completed: {Completed}", 
            enrollmentId, lessonId, completed);

        return progress;
    }

    public async Task<LessonProgress?> GetLessonProgressAsync(int enrollmentId, int lessonId)
    {
        return await _context.LessonProgresses
            .Include(lp => lp.Lesson)
            .Include(lp => lp.Enrollment)
            .FirstOrDefaultAsync(lp => lp.EnrollmentID == enrollmentId && lp.LessonID == lessonId);
    }

    public async Task<IEnumerable<LessonProgress>> GetEnrollmentProgressAsync(int enrollmentId)
    {
        return await _context.LessonProgresses
            .Where(lp => lp.EnrollmentID == enrollmentId)
            .Include(lp => lp.Lesson)
                .ThenInclude(l => l.Module)
            .OrderBy(lp => lp.Lesson.Module.ModuleOrder)
                .ThenBy(lp => lp.Lesson.LessonOrder)
            .ToListAsync();
    }

    #endregion

    #region Video Progress

    public async Task<bool> SaveVideoTimestampAsync(int enrollmentId, int lessonId, int timestamp)
    {
        var progress = await _context.LessonProgresses
            .FirstOrDefaultAsync(lp => lp.EnrollmentID == enrollmentId && lp.LessonID == lessonId);

        if (progress == null)
        {
            progress = new LessonProgress
            {
                EnrollmentID = enrollmentId,
                LessonID = lessonId,
                VideoTimestamp = timestamp
            };
            progress.UpdateProgress(timestamp);
            _context.LessonProgresses.Add(progress);
        }
        else
        {
            progress.UpdateProgress(timestamp);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int?> GetVideoTimestampAsync(int enrollmentId, int lessonId)
    {
        var progress = await _context.LessonProgresses
            .FirstOrDefaultAsync(lp => lp.EnrollmentID == enrollmentId && lp.LessonID == lessonId);

        return progress?.VideoTimestamp;
    }

    public async Task<bool> MarkVideoLessonCompleteAsync(int enrollmentId, int lessonId)
    {
        var lesson = await _context.Lessons.FindAsync(lessonId);
        if (lesson?.LessonType != LessonTypes.Video)
        {
            return false;
        }

        await UpdateLessonProgressAsync(enrollmentId, lessonId, completed: true);
        return true;
    }

    #endregion

    #region Progress Calculations

    public async Task<decimal> CalculateCourseProgressAsync(int enrollmentId)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.Course)
                .ThenInclude(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
            .Include(e => e.LessonProgresses)
            .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);

        if (enrollment == null)
        {
            return 0;
        }

        var totalLessons = enrollment.Course.Modules.SelectMany(m => m.Lessons).Count();
        if (totalLessons == 0)
        {
            return 0;
        }

        var completedLessons = enrollment.LessonProgresses.Count(lp => lp.IsCompleted);
        return (decimal)completedLessons / totalLessons * 100;
    }

    public async Task<decimal> CalculateModuleProgressAsync(int enrollmentId, int moduleId)
    {
        var module = await _context.Modules
            .Include(m => m.Lessons)
            .FirstOrDefaultAsync(m => m.ModuleID == moduleId);

        if (module == null || !module.Lessons.Any())
        {
            return 0;
        }

        var lessonIds = module.Lessons.Select(l => l.LessonID).ToList();
        var completedLessons = await _context.LessonProgresses
            .CountAsync(lp => lp.EnrollmentID == enrollmentId && 
                             lessonIds.Contains(lp.LessonID) && 
                             lp.IsCompleted);

        return (decimal)completedLessons / module.Lessons.Count * 100;
    }

    public async Task<CourseProgressSummary> GetCourseProgressSummaryAsync(int enrollmentId)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.Course)
                .ThenInclude(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
            .Include(e => e.LessonProgresses)
                .ThenInclude(lp => lp.Lesson)
            .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);

        if (enrollment == null)
        {
            throw new ArgumentException("Enrollment not found", nameof(enrollmentId));
        }

        var totalLessons = enrollment.Course.Modules.SelectMany(m => m.Lessons).Count();
        var completedLessons = enrollment.LessonProgresses.Count(lp => lp.IsCompleted);
        var progressPercentage = totalLessons > 0 ? (decimal)completedLessons / totalLessons * 100 : 0;

        var totalModules = enrollment.Course.Modules.Count;
        var completedModules = 0;

        foreach (var module in enrollment.Course.Modules)
        {
            var moduleProgress = await CalculateModuleProgressAsync(enrollmentId, module.ModuleID);
            if (moduleProgress >= 100)
            {
                completedModules++;
            }
        }

        var lastAccessedProgress = enrollment.LessonProgresses
            .OrderByDescending(lp => lp.LastAccessedAt)
            .FirstOrDefault();

        var isCompleted = await IsCourseCompletedAsync(enrollmentId);
        var completionDate = isCompleted ? await GetCourseCompletionDateAsync(enrollmentId) : null;

        return new CourseProgressSummary
        {
            EnrollmentId = enrollmentId,
            TotalLessons = totalLessons,
            CompletedLessons = completedLessons,
            ProgressPercentage = progressPercentage,
            TotalModules = totalModules,
            CompletedModules = completedModules,
            LastAccessedAt = lastAccessedProgress?.LastAccessedAt,
            LastAccessedLesson = lastAccessedProgress?.Lesson,
            IsCompleted = isCompleted,
            CompletionDate = completionDate
        };
    }

    #endregion

    #region Resume Functionality

    public async Task<Lesson?> GetLastAccessedLessonAsync(int enrollmentId)
    {
        var lastProgress = await _context.LessonProgresses
            .Where(lp => lp.EnrollmentID == enrollmentId)
            .Include(lp => lp.Lesson)
                .ThenInclude(l => l.Module)
            .OrderByDescending(lp => lp.LastAccessedAt)
            .FirstOrDefaultAsync();

        return lastProgress?.Lesson;
    }

    public async Task<LessonResumeInfo?> GetLessonResumeInfoAsync(int enrollmentId, int lessonId)
    {
        var progress = await GetLessonProgressAsync(enrollmentId, lessonId);
        if (progress?.Lesson == null)
        {
            return null;
        }

        return new LessonResumeInfo
        {
            Lesson = progress.Lesson,
            VideoTimestamp = progress.VideoTimestamp,
            IsCompleted = progress.IsCompleted,
            LastAccessedAt = progress.LastAccessedAt,
            CanResume = progress.VideoTimestamp.HasValue && !progress.IsCompleted
        };
    }

    #endregion

    #region Completion Tracking

    public async Task<bool> IsLessonCompletedAsync(int enrollmentId, int lessonId)
    {
        var progress = await _context.LessonProgresses
            .FirstOrDefaultAsync(lp => lp.EnrollmentID == enrollmentId && lp.LessonID == lessonId);

        return progress?.IsCompleted == true;
    }

    public async Task<bool> IsModuleCompletedAsync(int enrollmentId, int moduleId)
    {
        var moduleProgress = await CalculateModuleProgressAsync(enrollmentId, moduleId);
        return moduleProgress >= 100;
    }

    public async Task<bool> IsCourseCompletedAsync(int enrollmentId)
    {
        var courseProgress = await CalculateCourseProgressAsync(enrollmentId);
        return courseProgress >= 100;
    }

    public async Task<DateTime?> GetCourseCompletionDateAsync(int enrollmentId)
    {
        var enrollment = await _context.Enrollments.FindAsync(enrollmentId);
        return enrollment?.CompletionDate;
    }

    #endregion

    #region Private Helper Methods

    private async Task CheckAndUpdateCourseCompletionAsync(int enrollmentId)
    {
        var isCompleted = await IsCourseCompletedAsync(enrollmentId);
        if (!isCompleted)
        {
            return;
        }

        var enrollment = await _context.Enrollments.FindAsync(enrollmentId);
        if (enrollment != null && enrollment.CompletionDate == null)
        {
            enrollment.CompletionDate = DateTime.UtcNow;
            enrollment.Status = Models.EnrollmentStatus.Completed;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Course completed: Enrollment {EnrollmentId}", enrollmentId);

            // Trigger certificate generation
            try
            {
                await _certificateService.Value.CheckAndGenerateCertificateAsync(enrollment.UserID, enrollment.CourseID);
                _logger.LogInformation("Certificate generation triggered for User {UserId}, Course {CourseId}", 
                    enrollment.UserID, enrollment.CourseID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating certificate for User {UserId}, Course {CourseId}", 
                    enrollment.UserID, enrollment.CourseID);
                // Don't throw - certificate generation failure shouldn't prevent course completion
            }
        }
    }

    #endregion
}