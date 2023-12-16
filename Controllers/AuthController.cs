using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using System.Runtime.CompilerServices;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.Extensions;
using WebSchoolPlanner.IdentityProviders;
using WebSchoolPlanner.Models;
using WebSchoolPlanner.Options;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

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

    private readonly IEnumerable<string> _2faReasons = new[]
    {
        "disable2fa",
        "create2faRecovery",
        "remove2faRecovery"
    };

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
            return this.RedirectToReturnUrl(returnUrl);

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
            return this.RedirectToReturnUrl(returnUrl);

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

            IdentityResult updateResult = await _userManager.SetLastLoginAsync(user, DateTime.UtcNow);
            if (!updateResult.Succeeded)
                _logger.LogError("An error happened while update the last login date of user {0}", user.Id);

            // successful
            _logger.LogInformation("Login from IPv4 {0} to user {1}", clientIP, user.Id);
            return this.RedirectToReturnUrl(returnUrl);
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
    public async Task<IActionResult> Validate2fa([FromQuery(Name = "r")] string? returnUrl, [FromQuery(Name = "y")] string? reason)
    {
        User? user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        user ??= await _userManager.GetUserAsync(User);

        if (!_2faReasons.Contains(reason))     // Check only the 2fa conditions if it isn't a confirmation request
        {
            if (await CheckMfaConditionsAsync(user, returnUrl) is IActionResult result)
                return result;
        }
        else
            ViewBag.ConfirmationReason = reason;

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("2fa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validate2fa([FromQuery(Name = "r")] string? returnUrl, [FromQuery(Name = "y")] string? reason, [FromForm] Validate2faModel model)
    {
        User? user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        user ??= await _userManager.GetUserAsync(User);

        if (user is null)     // Requires an authenticated user
        {
            await HttpContext.ForbidAsync();
            return StatusCode(Response.StatusCode);
        }

        ViewBag.ConfirmationReason = reason;
        model.Code = model.Code.Replace(" ", string.Empty);
        if (ModelState.IsValid)
        {
            // Handles the reason of a 2fa confirmation
            if (!string.IsNullOrEmpty(reason))
            {
                if (await _userManager.VerifyTwoFactorAsync(user, model.TwoFactorMethod, model.Code))     // Success
                {
                    IActionResult handledConfirmation = await Handle2faConfirmationRequest(user!, returnUrl, reason);
                    _logger.LogInformation("2fa confirmation for reason '{0}' succeeded for user {1}", reason, user!.Id);
                    return handledConfirmation;
                }

                // Confirmation failed
                _logger.LogInformation("2fa confirmation for reason '{0}' failed for user {1}", reason, user!.Id);
                ViewBag.IsLoginFailed = true;
                return View();
            }

            if (await CheckMfaConditionsAsync(user, returnUrl) is IActionResult result)
                return result;

            SignInResult signInResult = await _signInManager.TwoFactorSignInAsync(model.TwoFactorMethod, model.Code, model.RememberMe);
            if (signInResult.Succeeded)     // Success
            {
                IdentityResult updateResult = await _userManager.SetLastLoginAsync(user, DateTime.UtcNow);
                if (!updateResult.Succeeded)
                    _logger.LogError("An error happened while update the last login date of user {0}", user.Id);

                _logger.LogInformation("2fa login from user {0}", user!.Id);
                return this.RedirectToReturnUrl(returnUrl);
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
    /// Handles the reason of a 2fa confirmation request
    /// </summary>
    /// <param name="user">The user</param>
    /// <param name="returnUrl">The url where the request should return after a successful execution</param>
    /// <param name="reason">The reason</param>
    /// <returns><see langword="null"/> if no special result is required.</returns>
    [NonAction]
    private async Task<IActionResult> Handle2faConfirmationRequest(User user, string? returnUrl, string reason)
    {
        IdentityResult result = IdentityResult.Success;
        switch (reason)
        {
            case "disable2fa":     // Disable 2fa feature and remove the totp secret
                result = await _userManager.SetTwoFactorEnabledAsync(user, false);
                if (!result.Succeeded)
                    break;

                // Remove every secret
                result = await _userManager.RemoveTwoFactorRecoveryCodesAsync(user, HttpContext.RequestServices);
                if (!result.Succeeded)
                    break;

                result = await _userManager.RemoveTwoFactorSecretAsync(user, HttpContext.RequestServices);
                if (!result.Succeeded)
                    break;

                result = await _userManager.RemoveEmailTwoFactorTokenAsync(user, HttpContext.RequestServices);
                if (!result.Succeeded)
                    break;

                if (await _signInManager.IsTwoFactorClientRememberedAsync(user))
                    await _signInManager.ForgetTwoFactorClientAsync();
                _logger.LogInformation("2fa feature for user {0} disabled", user.Id);

                result = await _userManager.RemoveTwoFactorRecoveryCodesAsync(user, HttpContext.RequestServices);
                break;
            case "create2faRecovery":
                (IActionResult? resultView, IdentityResult error) = await Create2faRecoveryAsync(user, returnUrl);
                if (error.Succeeded)
                    return resultView!;
                result = error;
                break;
            case "remove2faRecovery":
                result = await _userManager.RemoveTwoFactorRecoveryCodesAsync(user, HttpContext.RequestServices);
                break;
            default:
                throw new ArgumentException("The specified confirmation reason could not be found.", nameof(reason));
        }

        // If the execution wasn't successful log it and throw an exception
        if (!result.Succeeded)
        {
            string errorJson = JsonConvert.SerializeObject(result.Errors);
            _logger.LogError("An occurred error happened while executing 2fa confirmation reason '{1}'; User: {0}; Error: {2}", user.Id, reason, errorJson);
            throw new Exception(string.Format("An occurred error happened while executing 2fa confirmation reason '{0}'", reason));
        }

        return this.RedirectToReturnUrl(returnUrl);
    }

    [NonAction]
    private async Task<(IActionResult? result, IdentityResult error)> Create2faRecoveryAsync(User user, string? returnUrl)
    {
        MfaRecoveryOptions options = HttpContext.RequestServices.GetService<IOptions<MfaRecoveryOptions>>()!.Value;

        IEnumerable<string>? codes = await _userManager.GenerateTwoFactorRecoveryCodesAsync(user);
        if (codes is null)
        {
            IdentityResult failedResult = IdentityResult.Failed(new IdentityError
            {
                Code = nameof(Create2faRecoveryAsync),
                Description = "An error happened while creating new 2fa recovery codes"
            });
            return (null, failedResult);
        }

        // Code display
        ViewBag.ReturnUrl = returnUrl;
        IActionResult view = View("Create2faRecovery", codes!);
        return (view, IdentityResult.Success);
    }

    /// <summary>
    /// Checks the conditions to validate users 2fa
    /// </summary>
    /// <param name="user">The user to validate</param>
    /// <param name="returnUrl">The url where the request wasn't to return after a successfully validation</param>
    /// <returns>The result. <see langword="null"/> if the check was successful</returns>
    [NonAction]
    private async Task<IActionResult?> CheckMfaConditionsAsync(User? user, string? returnUrl)
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
            return this.RedirectToReturnUrl(returnUrl);

        return null;
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