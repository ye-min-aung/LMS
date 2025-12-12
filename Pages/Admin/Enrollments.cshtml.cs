using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Pages.Admin;

[Authorize(Roles = "Admin")]
public class EnrollmentsModel : PageModel
{
    private readonly LMSDbContext _context;

    public EnrollmentsModel(LMSDbContext context)
    {
        _context = context;
    }

    public List<Enrollment> Enrollments { get; set; } = new();
    public SelectList? Users { get; set; }
    public SelectList? Courses { get; set; }

    [BindProperty]
    public int SelectedUserId { get; set; }

    [BindProperty]
    public int SelectedCourseId { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostEnrollAsync()
    {
        // Check if enrollment already exists
        var existing = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == SelectedUserId && e.CourseID == SelectedCourseId);

        if (existing != null)
        {
            // Update to approved if exists
            existing.Status = EnrollmentStatus.Approved;
            existing.EnrollmentDate = DateTime.UtcNow;
            TempData["Message"] = "Enrollment updated to Approved.";
        }
        else
        {
            // Create new enrollment
            var enrollment = new Enrollment
            {
                UserID = SelectedUserId,
                CourseID = SelectedCourseId,
                Status = EnrollmentStatus.Approved,
                EnrollmentDate = DateTime.UtcNow
            };
            _context.Enrollments.Add(enrollment);
            TempData["Message"] = "User enrolled successfully!";
        }

        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeAsync(int enrollmentId)
    {
        var enrollment = await _context.Enrollments.FindAsync(enrollmentId);
        if (enrollment != null)
        {
            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Enrollment revoked.";
        }
        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        Enrollments = await _context.Enrollments
            .Include(e => e.User)
            .Include(e => e.Course)
            .OrderByDescending(e => e.EnrollmentDate)
            .ToListAsync();

        var users = await _context.Users
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, Display = u.FullName + " (" + u.Email + ")" })
            .ToListAsync();

        var courses = await _context.Courses
            .OrderBy(c => c.Title_EN)
            .Select(c => new { c.CourseID, Display = c.Title_EN + " - " + c.Price + " MMK" })
            .ToListAsync();

        Users = new SelectList(users, "Id", "Display");
        Courses = new SelectList(courses, "CourseID", "Display");
    }
}
