using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Pages.Admin.Courses;

[Authorize(Roles = "Admin")]
public class ModulesModel : PageModel
{
    private readonly LMSDbContext _context;

    public ModulesModel(LMSDbContext context)
    {
        _context = context;
    }

    public Course? Course { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Course = await _context.Courses
            .Include(c => c.Modules.OrderBy(m => m.ModuleOrder))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.LessonOrder))
            .FirstOrDefaultAsync(c => c.CourseID == id);

        if (Course == null)
        {
            return NotFound();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAddModuleAsync(int courseId, string Title_EN, string? Title_MM)
    {
        var course = await _context.Courses
            .Include(c => c.Modules)
            .FirstOrDefaultAsync(c => c.CourseID == courseId);

        if (course == null)
        {
            return NotFound();
        }

        var maxOrder = course.Modules.Any() ? course.Modules.Max(m => m.ModuleOrder) : 0;

        var module = new Module
        {
            CourseID = courseId,
            Title_EN = Title_EN,
            Title_MM = Title_MM ?? Title_EN,
            ModuleOrder = maxOrder + 1
        };

        _context.Modules.Add(module);
        await _context.SaveChangesAsync();

        return RedirectToPage(new { id = courseId });
    }

    public async Task<IActionResult> OnPostDeleteModuleAsync(int courseId, int moduleId)
    {
        var module = await _context.Modules.FindAsync(moduleId);
        if (module != null)
        {
            _context.Modules.Remove(module);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id = courseId });
    }

    public async Task<IActionResult> OnPostAddLessonAsync(
        int courseId, int moduleId, 
        string Title_EN, string? Title_MM, 
        string LessonType, string? ContentURL, string? Content_EN)
    {
        var module = await _context.Modules
            .Include(m => m.Lessons)
            .FirstOrDefaultAsync(m => m.ModuleID == moduleId);

        if (module == null)
        {
            return NotFound();
        }

        var maxOrder = module.Lessons.Any() ? module.Lessons.Max(l => l.LessonOrder) : 0;

        var lesson = new Lesson
        {
            ModuleID = moduleId,
            Title_EN = Title_EN,
            Title_MM = Title_MM ?? Title_EN,
            LessonType = LessonType,
            ContentURL = ContentURL,
            Content_EN = Content_EN,
            LessonOrder = maxOrder + 1
        };

        _context.Lessons.Add(lesson);
        await _context.SaveChangesAsync();

        return RedirectToPage(new { id = courseId });
    }

    public async Task<IActionResult> OnPostDeleteLessonAsync(int courseId, int lessonId)
    {
        var lesson = await _context.Lessons.FindAsync(lessonId);
        if (lesson != null)
        {
            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id = courseId });
    }
}
