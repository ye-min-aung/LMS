using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Pages.Admin.Courses;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly LMSDbContext _context;

    public IndexModel(LMSDbContext context)
    {
        _context = context;
    }

    public List<Course> Courses { get; set; } = new();

    public async Task OnGetAsync()
    {
        Courses = await _context.Courses
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .Include(c => c.Enrollments)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
}
