using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LMSPlatform.Services;

namespace LMSPlatform.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CertificateApiController : ControllerBase
{
    private readonly ICertificateService _certificateService;
    private readonly IUserService _userService;
    private readonly ILogger<CertificateApiController> _logger;

    public CertificateApiController(
        ICertificateService certificateService,
        IUserService userService,
        ILogger<CertificateApiController> logger)
    {
        _certificateService = certificateService;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("download/{certificateId:int}")]
    public async Task<IActionResult> DownloadCertificate(int certificateId)
    {
        try
        {
            var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
            if (user == null)
            {
                return Unauthorized();
            }

            var certificate = await _certificateService.GetCertificateAsync(certificateId);
            if (certificate == null)
            {
                return NotFound("Certificate not found");
            }

            // Check if user owns this certificate or is admin
            if (certificate.UserID != user.Id && user.Role != "Admin")
            {
                return Forbid();
            }

            var pdfBytes = await _certificateService.GenerateCertificatePdfAsync(certificate);
            var fileName = $"Certificate_{certificate.UniqueCertID}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading certificate {CertificateId}", certificateId);
            return StatusCode(500, "Error generating certificate");
        }
    }

    [HttpGet("validate/{uniqueCertId}")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateCertificate(string uniqueCertId)
    {
        try
        {
            var certificate = await _certificateService.ValidateCertificateAsync(uniqueCertId);
            
            if (certificate == null)
            {
                return Ok(new { isValid = false, message = "Certificate not found" });
            }

            return Ok(new 
            { 
                isValid = true,
                certificateId = certificate.UniqueCertID,
                studentName = certificate.User.FullName,
                courseName = certificate.Course.GetTitle(),
                completionDate = certificate.CompletionDate.ToString("MMMM dd, yyyy"),
                issuedDate = certificate.IssuedDate.ToString("MMMM dd, yyyy"),
                finalGrade = certificate.FinalGrade
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating certificate {UniqueCertId}", uniqueCertId);
            return StatusCode(500, "Error validating certificate");
        }
    }

    [HttpPost("generate/{courseId:int}")]
    public async Task<IActionResult> GenerateCertificate(int courseId)
    {
        try
        {
            var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
            if (user == null)
            {
                return Unauthorized();
            }

            // Check if user is eligible
            if (!await _certificateService.IsEligibleForCertificateAsync(user.Id, courseId))
            {
                return BadRequest("You are not eligible for a certificate for this course");
            }

            var certificate = await _certificateService.GenerateCertificateAsync(user.Id, courseId);

            return Ok(new 
            { 
                success = true,
                certificateId = certificate.CertificateID,
                uniqueCertId = certificate.UniqueCertID,
                message = "Certificate generated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating certificate for User {UserId}, Course {CourseId}", 
                User.Identity?.Name, courseId);
            return StatusCode(500, "Error generating certificate");
        }
    }
}