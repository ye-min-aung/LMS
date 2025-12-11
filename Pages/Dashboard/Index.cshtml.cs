using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;
using LMSPlatform.Services;

namespace LMSPlatform.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly LMSDbContext _context;
    private readonly IUserService _userService;
    private readonly ILessonProgressService _progressService;
    private readonly ICertificateService _certificateService;

    public IndexModel(
        LMSDbContext context,
        IUserService userService,
        ILessonProgressService progressService,
        ICertificateService certificateService)
    {
        _context = context;
        _userService = userService;
        _progressService = progressService;
        _certificateService = certificateService;
    }

    public new User? User { get; set; }
    public int EnrolledCourses { get; set; }
    public int CompletedCourses { get; set; }
    public int InProgressCourses { get; set; }
    public int CertificatesEarned { get; set; }
    public int TotalLessonsCompleted { get; set; }
    public int QuizzesPassed { get; set; }
    public int AssignmentsSubmitted { get; set; }
    public List<EnrollmentProgress> ActiveEnrollments { get; set; } = new();
    public List<Models.Certificate> RecentCertificates { get; set; } = new();

    public async Task OnGetAsync()
    {
        User = await _userService.GetUserByEmailAsync(base.User.Identity?.Name ?? "");
        if (User == null) return;

        // Get enrollment stats
        var enrollments = await _context.Enrollments
            .Where(e => e.UserID == User.Id && e.Status == Models.EnrollmentStatus.Approved)
            .Include(e => e.Course)
                .ThenInclude(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
            .Include(e => e.LessonProgresses)
            .ToListAsync();

        EnrolledCourses = enrollments.Count;
        CompletedCourses = enrollments.Count(e => e.Status == Models.EnrollmentStatus.Completed);
        InProgressCourses = enrollments.Count(e => e.Status == Models.EnrollmentStatus.Approved);

        // Get certificates
        var certificates = await _certificateService.GetUserCertificatesAsync(User.Id);
        CertificatesEarned = certificates.Count();
        RecentCertificates = certificates.OrderByDescending(c => c.IssuedDate).Take(2).ToList();

        // Calculate total lessons completed
        TotalLessonsCompleted = enrollments.Sum(e => e.LessonProgresses.Count(lp => lp.IsCompleted));

        // Get quiz stats
        QuizzesPassed = await _context.QuizAttempts
            .CountAsync(qa => qa.UserID == User.Id && qa.Passed);

        // Get assignment stats
        AssignmentsSubmitted = await _context.AssignmentSubmissions
            .CountAsync(asub => asub.UserID == User.Id);

        // Build active enrollments with progress
        foreach (var enrollment in enrollments.Where(e => e.Status == Models.EnrollmentStatus.Approved).Take(5))
        {
            var totalLessons = enrollment.Course.Modules.SelectMany(m => m.Lessons).Count();
            var completedLessons = enrollment.LessonProgresses.Count(lp => lp.IsCompleted);
            var progressPercentage = totalLessons > 0 ? (decimal)completedLessons / totalLessons * 100 : 0;

            // Get last accessed lesson
            var lastProgress = enrollment.LessonProgresses
                .OrderByDescending(lp => lp.LastAccessedAt)
                .FirstOrDefault();

            Lesson? lastLesson = null;
            Lesson? nextLesson = null;

            if (lastProgress != null)
            {
                lastLesson = await _context.Lessons.FindAsync(lastProgress.LessonID);
            }

            // Find next incomplete lesson
            var allLessons = enrollment.Course.Modules
                .OrderBy(m => m.ModuleOrder)
                .SelectMany(m => m.Lessons.OrderBy(l => l.LessonOrder))
                .ToList();

            var completedLessonIds = enrollment.LessonProgresses
                .Where(lp => lp.IsCompleted)
                .Select(lp => lp.LessonID)
                .ToHashSet();

            nextLesson = allLessons.FirstOrDefault(l => !completedLessonIds.Contains(l.LessonID));

            ActiveEnrollments.Add(new EnrollmentProgress
            {
                Enrollment = enrollment,
                Course = enrollment.Course,
                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                ProgressPercentage = progressPercentage,
                LastLesson = lastLesson,
                NextLesson = nextLesson
            });
        }
    }
}

public class EnrollmentProgress
{
    public Enrollment? Enrollment { get; set; }
    public Course? Course { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public decimal ProgressPercentage { get; set; }
    public Lesson? LastLesson { get; set; }
    public Lesson? NextLesson { get; set; }
}
