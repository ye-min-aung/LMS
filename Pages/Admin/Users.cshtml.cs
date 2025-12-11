using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly LMSDbContext _context;

    public UsersModel(LMSDbContext context)
    {
        _context = context;
    }

    public List<User> Users { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? RoleFilter { get; set; }

    public async Task OnGetAsync(string? search, string? role)
    {
        SearchTerm = search;
        RoleFilter = role;

        var query = _context.Users
            .Include(u => u.Enrollments)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => 
                u.FullName.Contains(search) || 
                u.Email!.Contains(search));
        }

        if (!string.IsNullOrEmpty(role))
        {
            query = query.Where(u => u.Role == role);
        }

        Users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsApprovedStudent = true;
            await _context.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
