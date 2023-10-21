using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebSchoolPlanner.Controllers;

/// <summary>
/// Controller for views for GDPR
/// </summary>
[Controller]
[AllowAnonymous]
[Route(RoutePrefix + "Privacy/")]
public sealed class PrivacyController : Controller
{
    /// <summary>
    /// A view for the cookie policy
    /// </summary>
    [Route("CookiePolicy")]
    public IActionResult CookiePolicy()
    {
        return View();
    }
}
