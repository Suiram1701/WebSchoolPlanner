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
using System.Runtime.CompilerServices;
using WebSchoolPlanner.Extensions;
using WebSchoolPlanner.Authorization;
using Microsoft.AspNetCore.Http.Features;

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

    public AuthController(ILogger<AuthController> logger, IConfiguration configuration, SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _logger = logger;
        _configuration = configuration;
        _signInManager = signInManager;
        _userManager = userManager;
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
        if (_signInManager.IsSignedIn(User))
            return ReturnRequestedUrl(returnUrl);

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

        if (_signInManager.IsSignedIn(User))     // Already signed in
            return ReturnRequestedUrl(returnUrl);

        if (ModelState.IsValid)
        {
            // Check if the user exists
            if (await _userManager.FindByNameAsync(model.Username) is not User user)
            {
                ViewBag.IsLoginFailed = true;
                return View(nameof(Login), model);
            }

            // Sign in
            SignInResult result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
            if (result.RequiresTwoFactor)     // Redirect to 2FA validation
                return RedirectToAction(nameof(Validate2fa), new { r = returnUrl });
            else if (result.IsLockedOut)
            {
                IActionResult? lockOutResult = await LockOutIfNecessaryAsync(user, model);
                if (lockOutResult is not null)
                    return lockOutResult;
            }
            else if (result.IsNotAllowed)
            {
                ViewBag.IsNotAllowed = true;
                return View(model);
            }
            else if (!result.Succeeded)     // Invalid / passwd
            {
                _logger.LogInformation("User {0} invalid password from IPv4 {1}", user.Id, clientIP);
                await _userManager.AccessFailedAsync(user);
                if (user.AccessFailedCount >= _userManager.Options.Lockout.MaxFailedAccessAttempts)     // Log if the user locked out
                    _logger.LogInformation("User {0} invalid password lockout", user.Id);

                ViewBag.IsLoginFailed = true;
                IActionResult? lockOutResult = await LockOutIfNecessaryAsync(user, model);
                if (lockOutResult is not null)
                    return lockOutResult;
                return View(model);
            }

            // successful
            _logger.LogInformation("Login from IPv4 {0} to user {1}", clientIP, user.Id);
            return ReturnRequestedUrl(returnUrl);
        }

        // Invalid model state
        _logger.LogInformation("Invalid login model state from IPv4 {0}", clientIP);
        ViewBag.IsInvalidState = true;
        return View(nameof(Login), model);
    }

    /// <summary>
    /// Returns a actionResult for a view that contains the lock out end date
    /// </summary>
    /// <param name="user">The user to check</param>
    /// <param name="model">The model to return to the view</param>
    /// <param name="viewName">The name of the view to return (Not not use!)</param>
    /// <returns><see langword="null"/> if no lock out is necessary</returns>
    [NonAction]
    private async Task<IActionResult?> LockOutIfNecessaryAsync(User user, object? model, [CallerMemberName] string? viewName = "")
    {
        DateTimeOffset? lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
        if (lockoutEnd?.DateTime > DateTime.UtcNow)
        {
            if (viewName == string.Empty)
                throw new ArgumentNullException(nameof(viewName), "The name of a in the controller contained view was expected.");

            ViewBag.LockOutEnd = lockoutEnd;
            return View(viewName, model);     // Display lockout msg
        }

        return null;
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("2fa")]
    public async Task<IActionResult> Validate2fa([FromQuery(Name = "r")] string? returnUrl)
    {
        User? user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        user ??= await _userManager.GetUserAsync(User);

        if (await CheckMfaConditions(user, returnUrl) is IActionResult result)
            return result;

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("2fa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validate2fa([FromQuery(Name = "r")] string? returnUrl, [FromForm] Validate2faModel model)
    {
        User? user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (await CheckMfaConditions(user, returnUrl) is IActionResult result)
            return result;

        IPAddress clientIP = HttpContext.Connection.RemoteIpAddress!.MapToIPv4();
        if (ModelState.IsValid)
        {
            SignInResult signInResult = await _signInManager.TwoFactorAuthenticatorSignInAsync(model.Code, false, model.RememberMe);
            if (signInResult.Succeeded)
            {
                _logger.LogInformation("2fa login from user {0}", user!.Id);
                return ReturnRequestedUrl(returnUrl);
            }
            else if (signInResult.IsLockedOut)
                return (await LockOutIfNecessaryAsync(user!, model))!;
            else     // General failed
            {
                _logger.LogInformation("2fa login failed from user {0}", user!.Id);
                ViewBag.IsLoginFailed = true;
                return View(model);
            }
        }

        _logger.LogInformation("Invalid 2fa login model state from user {0}", user!.Id);
        ViewBag.IsInvalidState = true;
        return View();
    }

    /// <summary>
    /// Checks the conditions to validate users 2fa
    /// </summary>
    /// <param name="user">The user to validate</param>
    /// <param name="returnUrl">The url where the request wasn't to return after a successfully validation</param>
    /// <returns>The result. <see langword="null"/> if the check was successful</returns>
    [NonAction]
    private async Task<IActionResult?> CheckMfaConditions(User? user, string? returnUrl)
    {
        // Not authenticated user
        if (user is null)
        {
            await HttpContext.ChallengeAsync();
            return StatusCode(Response.StatusCode);
        }

        // Forbid if 2fa not enabled
        if (!await _signInManager.IsTwoFactorEnabledAsync(user))
        {
            await HttpContext.ForbidAsync();
            return StatusCode(Response.StatusCode);
        }

        if (_signInManager.IsSignedIn(User))     // Already 2fa signed in
            return ReturnRequestedUrl(returnUrl);

        return null;
    }

    /// <summary>
    /// Returns a redirect the requested url, but if the url isn't local a redirect to the dashboard would executed.
    /// </summary>
    /// <param name="returnUrl">The requested url. If null a dashboard redirect would be executed</param>
    /// <returns>The redirect</returns>
    [NonAction]
    private IActionResult ReturnRequestedUrl(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
            return RedirectToAction("Index", "Dashboard");

        bool isValid = Uri.TryCreate(returnUrl, default(UriCreationOptions), out Uri? uri);
        isValid &= !uri?.IsAbsoluteUri ?? false;

        if (!isValid)
            return RedirectToAction("Index", "Dashboard");     // Redirect to dashboard
        return Redirect(returnUrl);
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("logout")]
    public async Task<IActionResult> Logout()
    {
        if (_signInManager.IsSignedIn(User))     // Normal sign out
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {0} logout", _userManager.GetUserId(User));
            return RedirectToAction(nameof(Login));
        }

        AuthenticateResult mfaIdResult = await HttpContext.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme);
        if (mfaIdResult.Succeeded)
            await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);

        return RedirectToAction(nameof(Login));
    }
}