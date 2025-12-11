using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;
using LMSPlatform.Services;

namespace LMSPlatform.Pages.Courses;

public class DetailsModel : PageModel
{
    private readonly LMSDbContext _context;
    private readonly IUserService _userService;
    private readonly ICourseService _courseService;

    public DetailsModel(LMSDbContext context, IUserService userService, ICourseService courseService)
    {
        _context = context;
        _userService = userService;
        _courseService = courseService;
    }

    public Course? Course { get; set; }
    public bool IsEnrolled { get; set; }
    public decimal ProgressPercentage { get; set; }
    public int TotalLessons { get; set; }
    public int VideoLessons { get; set; }
    public int TextLessons { get; set; }
    public int TotalQuizzes { get; set; }
    public int TotalAssignments { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Course = await _context.Courses
            .Include(c => c.Modules.OrderBy(m => m.ModuleOrder))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.LessonOrder))
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.CourseID == id);

        if (Course == null) return Page();

        var lessons = Course.Modules.SelectMany(m => m.Lessons).ToList();
        TotalLessons = lessons.Count;
        VideoLessons = lessons.Count(l => l.LessonType == LessonTypes.Video);
        TextLessons = lessons.Count(l => l.LessonType == LessonTypes.Text);

        TotalQuizzes = await _context.Quizzes.CountAsync(q => q.Lesson!.Module.CourseID == id);
        TotalAssignments = await _context.Assignments.CountAsync(a => a.Lesson!.Module.CourseID == id);

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userService.GetUserByEmailAsync(User.Identity.Name ?? "");
            if (user != null)
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserID == user.Id && e.CourseID == id && e.Status == Models.EnrollmentStatus.Approved);
                IsEnrolled = enrollment != null;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostEnrollAsync(int courseId)
    {
        if (!User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Account/Login", new { area = "Identity" });

        var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
        if (user == null) return RedirectToPage();

        var course = await _context.Courses.FindAsync(courseId);
        if (course == null || course.Price > 0) return RedirectToPage();

        var existingEnrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == user.Id && e.CourseID == courseId);

        if (existingEnrollment == null)
        {
            var enrollment = new Enrollment
            {
                UserID = user.Id,
                CourseID = courseId,
                EnrollmentDate = DateTime.UtcNow,
                Status = Models.EnrollmentStatus.Approved
            };
            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("/Dashboard/Index");
    }
}