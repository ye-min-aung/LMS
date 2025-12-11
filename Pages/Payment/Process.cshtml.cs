using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LMSPlatform.Models;
using LMSPlatform.Services;

namespace LMSPlatform.Pages.Payment;

[Authorize]
public class ProcessModel : PageModel
{
    private readonly IPaymentService _paymentService;
    private readonly IUserService _userService;

    public ProcessModel(IPaymentService paymentService, IUserService userService)
    {
        _paymentService = paymentService;
        _userService = userService;
    }

    public Models.Payment? Payment { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Payment = await _paymentService.GetPaymentAsync(id);
        
        if (Payment == null)
        {
            ErrorMessage = "Payment not found";
            return Page();
        }

        // Verify the payment belongs to the current user
        var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
        if (user == null || Payment.Enrollment?.UserID != user.Id)
        {
            ErrorMessage = "Unauthorized access";
            return Page();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync(int paymentId)
    {
        var payment = await _paymentService.GetPaymentAsync(paymentId);
        if (payment == null)
        {
            TempData["ErrorMessage"] = "Payment not found";
            return RedirectToPage("/Courses/Index");
        }

        // Simulate successful payment (in production, this would be handled by KBZ Pay callback)
        var callback = new PaymentCallbackModel
        {
            TransactionId = payment.TransactionID,
            Status = "SUCCESS",
            Amount = payment.Amount,
            Signature = "demo_signature"
        };

        await _paymentService.ProcessPaymentCallbackAsync(callback);

        TempData["SuccessMessage"] = "Payment successful! You are now enrolled.";
        return RedirectToPage("/Dashboard/Index");
    }

    public async Task<IActionResult> OnPostCancelAsync(int paymentId)
    {
        await _paymentService.UpdatePaymentStatusAsync(paymentId, PaymentStatusEnum.Failed);
        
        TempData["ErrorMessage"] = "Payment cancelled";
        return RedirectToPage("/Courses/Index");
    }
}
