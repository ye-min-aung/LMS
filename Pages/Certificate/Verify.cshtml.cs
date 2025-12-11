using Microsoft.AspNetCore.Mvc.RazorPages;
using LMSPlatform.Models;
using LMSPlatform.Services;

namespace LMSPlatform.Pages.Certificate;

public class VerifyModel : PageModel
{
    private readonly ICertificateService _certificateService;

    public VerifyModel(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    public string? CertificateId { get; set; }
    public Models.Certificate? Certificate { get; set; }

    public async Task OnGetAsync(string? certId)
    {
        CertificateId = certId;
        
        if (!string.IsNullOrEmpty(certId))
        {
            Certificate = await _certificateService.ValidateCertificateAsync(certId);
        }
    }
}
