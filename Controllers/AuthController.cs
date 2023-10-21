using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.Models;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using Humanizer;

namespace WebSchoolPlanner.Controllers;

/// <summary>
/// A Controller for authorization logic
/// </summary>
[Authorize]
[Controller]
[Route(RoutePrefix + "Auth/")]
public sealed class AuthController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    private readonly CultureInfo _uiCulture;

    public AuthController(SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _uiCulture = Thread.CurrentThread.CurrentUICulture;
    }

    /// <summary>
    /// A view for login
    /// </summary>
    /// <param name="returnUrl">The url to return after login</param>
    [AllowAnonymous]
    [Route("Login")]
    public IActionResult Login([FromQuery] string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    /// <summary>
    /// The action to login
    /// </summary>
    /// <param name="returnUrl">The url to return after login</param>
    /// <param name="model">The login data</param>
    [HttpPost]
    [AllowAnonymous]
    [Route("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromQuery] string? returnUrl, [FromForm] LoginModel? model)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (ModelState.IsValid && model is not null)
        {
            // Check if the user exists
            if (await _userManager.FindByEmailAsync(model.Email) is not User user)
            {
                ViewBag.IsLoginFailed = true;
                return View(nameof(Login), model);
            }

            SignInResult result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
            if (result.RequiresTwoFactor)     // Redirect to 2FA validation
                return RedirectToAction(nameof(TFAValidate), new
                {
                    languageCode = _uiCulture,
                    returnUrl
                });
            else if (result.IsLockedOut)
            {
                DateTimeOffset? lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                if (lockoutEnd?.DateTime > DateTime.UtcNow)
                {
                    ViewBag.LockOutEnd = lockoutEnd;
                    return View(nameof(Login), model);     // Display lockout msg
                }
            }
            else if (result.IsNotAllowed)
            {
                ViewBag.IsNotAllowed = true;
                return View(nameof(Login), model);
            }
            else if (!result.Succeeded)     // Invalid / passwd
            {
                await _userManager.AccessFailedAsync(user);

                ViewBag.IsLoginFailed = true;
                return View(nameof(Login), model);
            }

            if (returnUrl is not null)
                return Redirect(returnUrl);
            else
                return RedirectToAction(     // Redirect to dashboard
                    actionName: nameof(DashboardController.Index),
                    controllerName: "Dashboard",
                    routeValues: new { languageCode = _uiCulture });
        }

        ViewBag.IsInvalidState = true;
        return View(nameof(Login), model);
    }

    [AllowAnonymous]
    [Route("2FA")]
    public IActionResult TFAValidate([FromQuery] string? returnUrl)
    {
        throw new NotImplementedException();
    }

    [Route("Logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login), new { languageCode = _uiCulture });
    }
}
