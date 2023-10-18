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

    [AllowAnonymous]
    [Route("Login")]
    [SuppressMessage("Style", "IDE0060")]
    public IActionResult Login([FromQuery] string? returnUrl)
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginAsync([FromQuery] string? returnUrl, [FromForm] LoginModel? model)
    {
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

        return View(nameof(Login), model);
    }

    [AllowAnonymous]
    [Route("2FA")]
    public IActionResult TFAValidate([FromQuery] string? returnUrl)
    {
        throw new NotImplementedException();
    }

    [Route("Logout")]
    public async Task<IActionResult> LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login), new { languageCode = _uiCulture });
    }

    [Route("Lockout")]
    [AllowAnonymous]
    public async Task<IActionResult> Lockout()
    {
        User user = _userManager.Users.First();
        var a = await _userManager.SetLockoutEndDateAsync(user, new(DateTime.UtcNow.AddHours(12).AddMinutes(29)));

        return Ok();
    }

    [Route("EndLockout")]
    [AllowAnonymous]
    public async Task<IActionResult> EndLockout()
    {
        User user = _userManager.Users.First();
        var a = _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);

        return Ok();
    }
}
