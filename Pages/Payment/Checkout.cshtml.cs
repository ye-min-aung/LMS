using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;
using LMSPlatform.Services;

namespace LMSPlatform.Pages.Payment;

[Authorize]
public class CheckoutModel : PageModel
{
    private readonly LMSDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IUserService _userService;

    public CheckoutModel(LMSDbContext context, IPaymentService paymentService, IUserService userService)
    {
        _context = context;
        _paymentService = paymentService;
        _userService = userService;
    }

    public Course? Course { get; set; }
    public int TotalLessons { get; set; }

    public async Task<IActionResult> OnGetAsync(int courseId)
    {
        Course = await _context.Courses
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(c => c.CourseID == courseId);

        if (Course == null) return NotFound();

        TotalLessons = Course.Modules.Sum(m => m.Lessons.Count);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int courseId)
    {
        var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
        if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

        var result = await _paymentService.InitiatePaymentAsync(user.Id, courseId);

        if (result.Success && !string.IsNullOrEmpty(result.PaymentUrl))
        {
            return Redirect(result.PaymentUrl);
        }

        TempData["ErrorMessage"] = result.ErrorMessage ?? "Payment initiation failed";
        return RedirectToPage(new { courseId });
    }
}
