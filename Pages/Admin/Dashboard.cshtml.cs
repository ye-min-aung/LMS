using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    private readonly LMSDbContext _context;

    public DashboardModel(LMSDbContext context)
    {
        _context = context;
    }

    public int TotalUsers { get; set; }
    public int TotalCourses { get; set; }
    public int TotalEnrollments { get; set; }
    public int TotalCertificates { get; set; }
    public int PendingAssignments { get; set; }
    public int ActiveStudents { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<Enrollment> RecentEnrollments { get; set; } = new();
    public List<User> RecentUsers { get; set; } = new();

    public async Task OnGetAsync()
    {
        TotalUsers = await _context.Users.CountAsync();
        TotalCourses = await _context.Courses.CountAsync();
        TotalEnrollments = await _context.Enrollments.CountAsync();
        TotalCertificates = await _context.Certificates.CountAsync();
        
        PendingAssignments = await _context.AssignmentSubmissions
            .CountAsync(a => a.Status == "Submitted");
        
        ActiveStudents = await _context.Enrollments
            .Where(e => e.Status == Models.EnrollmentStatus.Approved)
            .Select(e => e.UserID)
            .Distinct()
            .CountAsync();

        TotalRevenue = await _context.Payments
            .Where(p => p.Status == PaymentStatusEnum.Completed)
            .SumAsync(p => p.Amount);

        RecentEnrollments = await _context.Enrollments
            .Include(e => e.User)
            .Include(e => e.Course)
            .OrderByDescending(e => e.EnrollmentDate)
            .Take(5)
            .ToListAsync();

        RecentUsers = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .ToListAsync();
    }
}
