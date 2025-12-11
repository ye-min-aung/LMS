using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly LMSDbContext _context;
    private readonly UserManager<User> _userManager;

    public EditModel(LMSDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public User? UserToEdit { get; set; }
    public List<Enrollment> UserEnrollments { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        UserToEdit = await _context.Users
            .Include(u => u.Enrollments)
                .ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (UserToEdit == null)
        {
            return NotFound();
        }

        UserEnrollments = UserToEdit.Enrollments.ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        int id, string FullName, string Role, bool IsApprovedStudent)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.FullName = FullName;
        user.Role = Role;
        user.IsApprovedStudent = IsApprovedStudent;

        // Update role in Identity
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, Role);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "User updated successfully.";
        return RedirectToPage("/Admin/Users");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Don't allow deleting the current admin
        if (User.Identity?.Name == user.Email)
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToPage(new { id });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "User deleted successfully.";
        return RedirectToPage("/Admin/Users");
    }
}
