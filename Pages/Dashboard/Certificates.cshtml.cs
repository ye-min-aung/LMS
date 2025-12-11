using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LMSPlatform.Models;
using LMSPlatform.Services;

namespace LMSPlatform.Pages.Dashboard;

[Authorize]
public class CertificatesModel : PageModel
{
    private readonly ICertificateService _certificateService;
    private readonly IUserService _userService;

    public CertificatesModel(
        ICertificateService certificateService,
        IUserService userService)
    {
        _certificateService = certificateService;
        _userService = userService;
    }

    public IEnumerable<Models.Certificate> Certificates { get; set; } = new List<Models.Certificate>();

    public async Task OnGetAsync()
    {
        var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
        if (user != null)
        {
            Certificates = await _certificateService.GetUserCertificatesAsync(user.Id);
        }
    }
}
