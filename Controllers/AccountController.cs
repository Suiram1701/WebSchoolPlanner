using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OtpNet;
using QRCoder;
using WebSchoolPlanner.Db.Models;
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable2fa([FromServices] IOptions<TotpAuthenticationOptions> options)
    {
        User user = (await _userManager.GetUserAsync(User))!;

        if (ModelState.IsValid)
        {
            string tokenProvider = _userManager.Options.Tokens.AuthenticatorTokenProvider;
            string token = await _userManager.GenerateTwoFactorTokenAsync(user, tokenProvider);

            byte[] secret = Convert.FromHexString(token);
            string base32Secret = Base32Encoding.ToString(secret);

            byte algorithmFlag = (byte)options.Value.Algorithm;
            OneTimePasswordAuthAlgorithm algorithm = (OneTimePasswordAuthAlgorithm)algorithmFlag;

            Payload payload = new OneTimePassword()
            {
                Type = OneTimePasswordAuthType.TOTP,
                Label = user.UserName,
                Issuer = options.Value.Issuer,
                Digits = options.Value.DigitsCount,
                Period = options.Value.ValidTimeSpan.Seconds,
                AuthAlgorithm = algorithm,
                Secret = base32Secret
            };
            using QRCodeGenerator qRCodeGenerator = new();
            using QRCodeData qrCodeData = qRCodeGenerator.CreateQrCode(payload, ECCLevel.Q);
            SvgQRCode svgQRCode = new(qrCodeData);
            ViewBag.QRCode = svgQRCode;

            return View();
        }

        ViewBag.IsInvalidState = true;
        return View();
    }

    [HttpPost]
    [Route("enable2fa")]
    [ValidateAntiForgeryToken]
    public IActionResult Enable2fa([FromForm] Enable2faModel model)
    {
        return View();
    }

    [HttpGet]
    [Route("settings")]
    public IActionResult Settings()
    {
        return View();
    }
}
