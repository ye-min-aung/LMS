using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Pages.Courses;

public class IndexModel : PageModel
{
    private readonly LMSDbContext _context;

    public IndexModel(LMSDbContext context)
    {
        _context = context;
    }

    public List<Course> Courses { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }

    public async Task OnGetAsync(string? search, string? category)
    {
        SearchTerm = search;
        Category = category;

        var query = _context.Courses
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .Include(c => c.Enrollments)
            .Where(c => c.IsPublished)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => 
                c.Title_EN.Contains(search) || 
                c.Title_MM.Contains(search) ||
                (c.Description_EN != null && c.Description_EN.Contains(search)) ||
                (c.Description_MM != null && c.Description_MM.Contains(search)));
        }

        Courses = await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
}
