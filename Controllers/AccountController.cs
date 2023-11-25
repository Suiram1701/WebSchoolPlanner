using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OtpNet;
using QRCoder;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.Extensions;
using WebSchoolPlanner.Models;
using WebSchoolPlanner.Options;
using static QRCoder.PayloadGenerator;
using static QRCoder.PayloadGenerator.OneTimePassword;
using static QRCoder.QRCodeGenerator;

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
    [Route("")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Route("enable2fa")]
    public async Task<IActionResult> Enable2fa()
    {
        User user = (await _userManager.GetUserAsync(User))!;
        if (await _signInManager.IsTwoFactorEnabledAsync(user))     // 2FA have to be disabled
        {
            await HttpContext.ForbidAsync();
            return StatusCode(Response.StatusCode);
        }
        
        string tokenProvider = _userManager.Options.Tokens.AuthenticatorTokenProvider;
        string token = await _userManager.GenerateTwoFactorTokenAsync(user, tokenProvider);

        byte[] secret = Convert.FromHexString(token);
        Create2faQRCode(user, secret);

        Enable2faModel model = new(secret);
        return View(model);
    }

    [HttpPost]
    [Route("enable2fa")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable2fa([FromForm] Enable2faModel model, [FromServices] IOptions<TotpAuthenticationOptions> options)
    {
        User user = (await _userManager.GetUserAsync(User))!;

        if (ModelState.IsValid)
        {
            byte[] secret = Convert.FromHexString(model.EncodedSecret);

            int timesteps = options.Value.ValidTimeSpan.Seconds;
            Totp otp = new(secret, timesteps, OtpHashMode.Sha1, options.Value.DigitsCount);
            if (otp.VerifyTotp(model.Code, out _))
            {
                IdentityResult setSecretResult = await _userManager.SetTwoFactorSecretAsync(user, secret);
                HandleIdentityResult(setSecretResult);

                IdentityResult setEnabledResult = await _userManager.SetTwoFactorEnabledAsync(user, true);
                HandleIdentityResult(setEnabledResult);

                // Successful
                _logger.LogInformation("2fa feature for user {0} enabled", user.Id);
                return RedirectToAction("Index", "Account");
            }
            else
            {
                // Invalid enable code
                Create2faQRCode(user, secret);
                ViewBag.IsInvalid = true;
                return View(model);
            }
        }

        // Invalid model state
        _logger.LogInformation("Invalid enable 2fa model state from user {0}", user.Id);
        ViewBag.IsInvalidState = true;
        throw new ArgumentException("The request model state is invalid", nameof(model));
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
    /// Throws a exception and log it when the setSecretResult isn't successful
    /// </summary>
    /// <param name="result">The setSecretResult</param>
    /// <exception cref="Exception"></exception>
    [NonAction]
    private void HandleIdentityResult(IdentityResult result)
    {
        if (result.Succeeded)
            return;

        string errorJson = JsonConvert.SerializeObject(result.Errors);
        _logger.LogError("An identity setSecretResult happened while enable 2fa of user {0}; setSecretResult: {1}", _userManager.GetUserId(User), errorJson);
        throw new Exception("An occurred setSecretResult happened while setting account settings.");
    }

    [HttpGet]
    [Route("settings")]
    public IActionResult Settings()
    {
        return View();
    }
}
