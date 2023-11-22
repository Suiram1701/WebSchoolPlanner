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
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace WebSchoolPlanner.Controllers;

/// <summary>
/// A Controller for authorization logic
/// </summary>
[Authorize]
[Controller]
[Route("auth/")]
public sealed class AuthController : Controller
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    private readonly CultureInfo _uiCulture;

    public AuthController(ILogger<AuthController> logger, IConfiguration configuration, SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _logger = logger;
        _configuration = configuration;
        _signInManager = signInManager;
        _userManager = userManager;
        _uiCulture = Thread.CurrentThread.CurrentUICulture;
    }

    /// <summary>
    /// A view for login
    /// </summary>
    /// <param name="returnUrl">The url to return after login</param>
    [HttpGet]
    [AllowAnonymous]
    [Route("login")]
    public IActionResult Login([FromQuery(Name = "r")] string? returnUrl)
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
    [Route("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromQuery(Name = "r")] string? returnUrl, [FromForm] LoginModel model)
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

            SignInResult result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (result.Succeeded || result.RequiresTwoFactor)
            {
                // MFA claims
                IList<Claim> claims = new List<Claim>();
                claims.Add(new("mfa_enabled", user.TwoFactorEnabled.ToString()));
                if (user.TwoFactorEnabled)
                    claims.Add(new("mfa_valid", result.RequiresTwoFactor.ToString()));

                // Login
                TimeSpan loginSpan = DetermineLoginSpan(model.RememberMe);
                AuthenticationProperties properties = new()
                {
                    IsPersistent = model.RememberMe,
                    AllowRefresh = true,
                    IssuedUtc = DateTimeOffset.UtcNow,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(loginSpan)
                };
                await _signInManager.SignInWithClaimsAsync(user, properties, claims);
            }

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
                _logger.LogInformation("User {0} invalid password from IPv4 {1}", user.Id, clientIP);
                await _userManager.AccessFailedAsync(user);
                if (user.AccessFailedCount >= _userManager.Options.Lockout.MaxFailedAccessAttempts)     // Log if the user locked out
                    _logger.LogInformation("User {0} invalid password lockout", user.Id);

                ViewBag.IsLoginFailed = true;
                return View(nameof(Login), model);
            }

            // successful
            _logger.LogInformation("Login from IPv4 {0} to user {1}", clientIP, user.Id);
            if (returnUrl is not null)
            {
                // Validate redirect URI
                bool isValid = Uri.TryCreate(returnUrl, default, out Uri? uri);
                isValid &= !uri?.IsAbsoluteUri ?? false;

                if (!isValid)
                    return RedirectToAction("Index", "Dashboard");     // Redirect to dashboard
                return Redirect(returnUrl);
            }
            else
                return RedirectToAction("Index", "Dashboard");     // Redirect to dashboard
        }

        // Invalid model state
        _logger.LogInformation("Invalid log in model state from IPv4 {0}", clientIP);
        ViewBag.IsInvalidState = true;
        return View(nameof(Login), model);
    }

    [NonAction]
    private TimeSpan DetermineLoginSpan(bool isPersistent)
    {
        string configurationSuffix = isPersistent
                    ? "Persistent"
                    : (Request.Path.StartsWithSegments("/api")
                        ? "Api"
                        : "Default"
                    );
        string? expiresString = _configuration[AuthenticationConfigurationPrefix + "Expires:" + configurationSuffix];
        if (!uint.TryParse(expiresString, out uint expiresSeconds))
            expiresSeconds = 3600;     // 1 hour
        return TimeSpan.FromSeconds(expiresSeconds);
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("2fa")]
    public IActionResult TFAValidate([FromQuery(Name = "r")] string? returnUrl)
    {
        throw new NotImplementedException();
    }

    [HttpGet]
    [Route("logout")]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User {0} logout", _userManager.GetUserId(User));
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }
}
