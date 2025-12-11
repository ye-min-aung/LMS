using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Pages.Admin.Courses;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly LMSDbContext _context;

    public CreateModel(LMSDbContext context)
    {
        _context = context;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(
        string Title_EN, string? Title_MY,
        string? Description_EN, string? Description_MY,
        decimal Price, string? ThumbnailURL, bool IsPublished)
    {
        var course = new Course
        {
            Title_EN = Title_EN,
            Title_MM = Title_MY ?? Title_EN,
            Description_EN = Description_EN,
            Description_MM = Description_MY,
            Price = Price,
            ThumbnailURL = ThumbnailURL,
            IsPublished = IsPublished,
            CreatedAt = DateTime.UtcNow
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Admin/Courses/Index");
    }
}
