using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OtpNet;
using QRCoder;
using System.Security.Claims;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.Extensions;
using WebSchoolPlanner.IdentityProviders;
using WebSchoolPlanner.IdentityProviders.EmailSenders;
using WebSchoolPlanner.Models;
using WebSchoolPlanner.Options;
using static QRCoder.PayloadGenerator;
using static QRCoder.PayloadGenerator.OneTimePassword;
using static QRCoder.QRCodeGenerator;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace WebSchoolPlanner.Controllers;

[Authorize]
[Route("account/")]
[Controller]
public sealed class AccountController : Controller
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public AccountController(ILogger<AccountController> logger, IConfiguration configuration, SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _logger = logger;
        _configuration = configuration;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Route("enable-2fa-app")]
    public async Task<IActionResult> EnableApp2fa([FromQuery(Name = "r")] string? returnUrl)
    {
        User user = (await _userManager.GetUserAsync(User))!;
        if (await _signInManager.IsTwoFactorEnabledAsync(user))     // 2FA have to be disabled
            return Forbid(IdentityConstants.ApplicationScheme);
        
        string tokenProvider = _userManager.Options.Tokens.AuthenticatorTokenProvider;
        string base32Secret = await _userManager.GenerateTwoFactorTokenAsync(user, tokenProvider);

        byte[] secret = Base32Encoding.ToBytes(base32Secret);
        Create2faQRCode(user, secret);
        EnableApp2faModel model = new(secret);

        ViewBag.ReturnUrl = returnUrl;
        return View(model);
    }

    [HttpPost]
    [Route("enable-2fa-app")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableApp2fa([FromQuery(Name = "r")] string? returnUrl, [FromForm] EnableApp2faModel model, [FromServices] IOptions<TotpAuthenticationOptions> options)
    {
        User user = (await _userManager.GetUserAsync(User))!;
        if (await _signInManager.IsTwoFactorEnabledAsync(user))     // 2FA have to be disabled
            return Forbid(IdentityConstants.ApplicationScheme);

        if (ModelState.IsValid)
        {
            if (await _userManager.VerifyTwoFactorAsync(user, TwoFactorMethod.App, model.Code))
            {
                IdentityResult updateResult = await _userManager.SetTwoFactorEnabledAsync(user, true);
                HandleEnable2faResult(updateResult, "app");

                // Successful
                _logger.LogInformation("2fa app feature for user {0} enabled", user.Id);
                return this.RedirectToReturnUrl(returnUrl);
            }
            else
            {
                // Invalid enable code
                byte[] secret = model.GetSecret();
                Create2faQRCode(user, secret);
                ViewBag.IsInvalid = true;
                model.Code = string.Empty;

                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }
        }

        // Invalid model state
        _logger.LogInformation("Invalid enable 2fa app model state from user {0}", user.Id);
        throw new ArgumentException("The request model state is invalid", nameof(model));
    }

    [HttpGet]
    [Route("enable-2fa-email")]
    public async Task<IActionResult> EnableEmail2fa([FromQuery(Name = "r")] string? returnUrl)
    {
        User user = (await _userManager.GetUserAsync(User))!;
        if (user.IsEmailTwoFactorEnabled)
            return Forbid(IdentityConstants.ApplicationScheme);

        return View();
    }

    [HttpPost]
    [Route("enable-2fa-email")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableEmail2fa([FromQuery(Name = "r")] string? returnUrl, [FromForm] Enable2faModel model)
    {
        User user = (await _userManager.GetUserAsync(User))!;
        if (user.IsEmailTwoFactorEnabled)     // Email 2fa is already enabled
            return Forbid(IdentityConstants.ApplicationScheme);

        if (ModelState.IsValid)
        {
            if (await _userManager.VerifyTwoFactorAsync(user, TwoFactorMethod.Email, model.Code))
            {
                IdentityResult updateResult = await _userManager.SetEmailTwoFactorEnabledAsync(user, true);
                HandleEnable2faResult(updateResult, "email");

                _logger.LogInformation("2fa email feature for user {0} enabled", user.Id);
                return this.RedirectToReturnUrl(returnUrl);
            }

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.IsInvalid = true;

            return View();
        }

        // Invalid model state
        _logger.LogInformation("Invalid enable 2fa email model state from user {0}", user.Id);
        throw new ArgumentException("The request model state is invalid", nameof(model));
    }

    [HttpGet]
    [Route("enable-2fa-email/send")]
    public async Task<IActionResult> EnableEmail2faSendEmail([FromServices] EmailSenderBase<User> emailSender)
    {
        User user = (await _userManager.GetUserAsync(User))!;
        if (user.IsEmailTwoFactorEnabled)
            return this.ApiForbidden("The signed in user isn't allowed to do this action");

        string code = await _userManager.GenerateEmailTwoFactorTokenAsync(user);
        await emailSender.SendTwoFactorCodeAsync(user, user.Email!, code);
        return Ok();
    }

    [HttpGet]
    [Route("forget-2fa")]
    public async Task<IActionResult> Forget2faRemembered([FromQuery(Name = "r")] string? returnUrl)
    {
        await _signInManager.ForgetTwoFactorClientAsync();
        return this.RedirectToReturnUrl(returnUrl);
    }

    /// <summary>
    /// Creates a 2fa qr code for the user and the secret and save it in the view bag
    /// </summary>
    /// <param name="user">The user</param>
    /// <param name="secret">The secret to encode</param>
    [NonAction]
    private void Create2faQRCode(User user, byte[] secret)
    {
        IOptions<TotpAuthenticationOptions> options = HttpContext.RequestServices.GetService<IOptions<TotpAuthenticationOptions>>()!;

        string base32Secret = Base32Encoding.ToString(secret);
        Payload payload = new OneTimePassword()
        {
            Type = OneTimePasswordAuthType.TOTP,
            Label = user.UserName,
            Issuer = options.Value.Issuer,
            Digits = options.Value.DigitsCount,
            Period = options.Value.ValidTimeSpan.Seconds,
            AuthAlgorithm = OneTimePasswordAuthAlgorithm.SHA1,
            Secret = base32Secret
        };
        using QRCodeGenerator qRCodeGenerator = new();
        QRCodeData qrCodeData = qRCodeGenerator.CreateQrCode(payload, ECCLevel.Q);
        ViewBag.QRCodeData = qrCodeData;
    }

    /// <summary>
    /// Throws a exception and log it when the <paramref name="result"/> isn't successful
    /// </summary>
    /// <param name="result">The setSecretResult</param>
    /// <param name="featureName">The name of the feature</param>
    [NonAction]
    private void HandleEnable2faResult(IdentityResult result, string featureName)
    {
        if (result.Succeeded)
            return;

        string errorJson = JsonConvert.SerializeObject(result.Errors);
        _logger.LogError("An identity error happened while enable 2fa {0} feature of user {1}; error: {2}", featureName, _userManager.GetUserId(User), errorJson);
        throw new Exception(string.Format("An occurred error happened while enable 2fa {0}.", featureName));
    }

    [HttpGet]
    [Route("settings")]
    public IActionResult Settings()
    {
        return View();
    }
}
