using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LMSPlatform.Services;

namespace LMSPlatform.Areas.Identity.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly IUserService _userService;

    public ForgotPasswordModel(IUserService userService)
    {
        _userService = userService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            await _userService.ResetPasswordAsync(Input.Email);

            // Don't reveal that the user does not exist or is not confirmed
            return RedirectToPage("./ForgotPasswordConfirmation");
        }

        return Page();
    }
}