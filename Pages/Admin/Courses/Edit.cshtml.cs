using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Pages.Admin.Courses;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly LMSDbContext _context;

    public EditModel(LMSDbContext context)
    {
        _context = context;
    }

    public Course? Course { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Course = await _context.Courses.FindAsync(id);
        if (Course == null)
        {
            return NotFound();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        int id,
        string Title_EN, string? Title_MY,
        string? Description_EN, string? Description_MY,
        decimal Price, string? ThumbnailURL, bool IsPublished)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound();
        }

        course.Title_EN = Title_EN;
        course.Title_MM = Title_MY ?? Title_EN;
        course.Description_EN = Description_EN;
        course.Description_MM = Description_MY;
        course.Price = Price;
        course.ThumbnailURL = ThumbnailURL;
        course.IsPublished = IsPublished;
        course.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToPage("/Admin/Courses/Index");
    }
}
