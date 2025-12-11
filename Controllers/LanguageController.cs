using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace LMSPlatform.Controllers;

[Route("[controller]/[action]")]
public class LanguageController : Controller
{
    [HttpGet]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        if (!string.IsNullOrEmpty(culture))
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions 
                { 
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                }
            );
        }

        return LocalRedirect(returnUrl ?? "/");
    }

    [HttpGet]
    public IActionResult GetCurrentCulture()
    {
        var culture = HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en";
        return Json(new { culture });
    }
}
