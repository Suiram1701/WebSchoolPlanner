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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Net;

namespace WebSchoolPlanner.Controllers;

/// <summary>
/// A Controller for authorization logic
/// </summary>
[Authorize]
[Controller]
[Route("Auth/")]
public sealed class AuthController : Controller
{
    private readonly ILogger _logger;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    private readonly CultureInfo _uiCulture;

    public AuthController(ILogger<AuthController> logger, SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _logger = logger;
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
    public async Task<IActionResult> Login([FromQuery(Name = "r")] string? returnUrl, [FromForm] LoginModel? model)
    {
        IPAddress clientIP = HttpContext.Connection.RemoteIpAddress!.MapToIPv4();
        ViewBag.ReturnUrl = returnUrl;

        if (ModelState.IsValid && model is not null)
        {
            // Check if the user exists
            if (await _userManager.FindByNameAsync(model.Username) is not User user)
            {
                ViewBag.IsLoginFailed = true;
                return View(nameof(Login), model);
            }

            SignInResult result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
            if (result.RequiresTwoFactor)     // Redirect to 2FA validation
                return RedirectToAction(nameof(TFAValidate), new { r = returnUrl });
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
                _logger.LogInformation("User {0} invalid password from IPv4 {1}");
                await _userManager.AccessFailedAsync(user);
                if (user.AccessFailedCount >= _userManager.Options.Lockout.MaxFailedAccessAttempts)     // Log if the user locked out
                    _logger.LogInformation("User {0} invalid password lockout", user.Id);

                ViewBag.IsLoginFailed = true;
                return View(nameof(Login), model);
            }

            // successful
            _logger.LogInformation("Login from IPv4 {0} to user {1}", clientIP, user.Id);
            if (returnUrl is not null)
                return Redirect(returnUrl);
            else
                return RedirectToAction("Index", "Dashboard");     // Redirect to dashboard
        }

        // Invalid model state
        _logger.LogInformation("Invalid model state from IPv4 {0}", clientIP);
        ViewBag.IsInvalidState = true;
        return View(nameof(Login), model);
    }

    [AllowAnonymous]
    [Route("2FA")]
    public IActionResult TFAValidate([FromQuery(Name = "r")] string? returnUrl)
    {
        throw new NotImplementedException();
    }

    [Route("Logout")]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User {0} logout", _userManager.GetUserId(User));
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }
}
