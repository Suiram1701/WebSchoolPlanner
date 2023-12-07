using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft;
using System.Text;
using WebSchoolPlanner.ApiModels;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.HttpResults;
using WebSchoolPlanner.Swagger.Attributes;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Threading.Tasks;
using WebSchoolPlanner.Db.Stores;
using System.Net;

namespace WebSchoolPlanner.ApiControllers.V1;

/// <summary>
/// Endpoints to manage the settings of the logged in account.
/// </summary>
[Route(ApiRoutePrefix + "account/settings")]
[Authorize]
[ApiVersion("1")]
[ApiController]
public sealed class AccountSettingsController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public AccountSettingsController(ILogger<AccountSettingsController> logger, SignInManager<User> signInManager, UserManager<User> userManager)
    {
        _logger = logger;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Returns the requested settings.
    /// </summary>
    /// <remarks>
    /// Returns the in the request specified settings from the currently logged in user.
    /// 
    /// Possible setting names are: "culture" Represents the selected language. Possible values are all by the server supported cultures in the ISO-3166 format; 
    /// "theme": Represents the selected color theme. Possible values are "White", "Dark" or "Auto".
    /// 
    /// Sample request:
    /// 
    ///     POST /api/v1/account/settings
    ///     [
    ///         "culture"
    ///     ]
    /// </remarks>
    /// <param name="settingNames">The setting names to return.</param>
    [HttpPost]
    [Consumes(typeof(string[]), "application/json")]
    [Produces("application/json", Type = typeof(Dictionary<string, string>))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, string>))]
    public async Task<IActionResult> PostSettings([FromBody] string[] settingNames)
    {
        User user = (await _userManager.GetUserAsync(User))!;
        IList<Claim> userClaims = await _userManager.GetClaimsAsync(user);

        // Read the setting values
        List<string> notFoundSettings = new();
        Dictionary<string, string> settingValues = new();
        foreach (string settingName in settingNames)
        {
            if (settingValues.ContainsKey(settingName))
                continue;

            string settingValue;
            switch (settingName)
            {
                case "culture":
                    settingValue = userClaims.FirstOrDefault(c => c.Type == ConfigClaimPrefix + "culture")?.Value ?? "en";
                    break;
                case "theme":
                    settingValue = userClaims.FirstOrDefault(c => c.Type == ConfigClaimPrefix + "theme")?.Value ?? Theme.Auto.ToString();
                    break;
                default:
                    notFoundSettings.Add(settingName);
                    continue;
            }

            settingValues.Add(settingName, settingValue);
        }

        // Return a error if any of the requested settings don't exist
        if (notFoundSettings.Any())
        {
            string notFoundSettingsString = string.Join(", ", notFoundSettings);
            throw new ArgumentException(string.Format("The settings {0} aren't found.", notFoundSettingsString), nameof(settingNames));
        }

        using StringWriter sw = new();
        using JsonWriter writer = new JsonTextWriter(sw);

        writer.WriteStartObject();
        foreach ((string key, string value) in settingValues)
        {
            writer.WritePropertyName(key);
            writer.WriteValue(value);
        }

        writer.WriteEndObject();

        return Ok(sw.ToString());
    }

    /// <summary>
    /// Sets for the specified settings the given value.
    /// </summary>
    /// <remarks>
    /// Sets for the specified settings a given string value.
    /// 
    /// Possible setting names are: "culture": Represents the selected language. Possible values are all by the server supported cultures in the ISO-3166 format; 
    /// "theme": Represents the selected color theme. Possible values are "White", "Dark" or "Auto".
    /// 
    /// Sample request:
    /// 
    ///     PUT /api/v1/account/settings
    ///     {
    ///         "culture": "de-DE",
    ///     }
    /// </remarks>
    /// <param name="localizationOptions"></param>
    /// <param name="settings">The settings to set.</param>
    [HttpPut]
    [Consumes(typeof(Dictionary<string, string>), "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PutSettings([FromBody] Dictionary<string, string> settings, [FromServices] IOptions<RequestLocalizationOptions> localizationOptions)
    {
        ValidateSettings(settings);

        User user = (await _userManager.GetUserAsync(User))!;
        IList<Claim> userClaims = await _userManager.GetClaimsAsync(user);

        List<Task<IdentityResult>> claimTasks = new();

        // Add all settings that don't exist
        IEnumerable<KeyValuePair<string, string>> settingsToAdd = settings
            .Select(s => new KeyValuePair<string, string>(ConfigClaimPrefix + s.Key, s.Value))
            .Where(s => !userClaims.Any(c => c.Type == s.Key));
        if (settingsToAdd.Any())
        {
            Task<IdentityResult> task = _userManager.AddClaimsAsync(user, settingsToAdd.Select(s => new Claim(s.Key, s.Value))).ContinueWith<IdentityResult>(CatchSettingsSaveError);
            claimTasks.Add(task);
        }

        // Replace the settings that already exists
        foreach ((string key, string value) in settings
            .Select(s => new KeyValuePair<string, string>(ConfigClaimPrefix + s.Key, s.Value))
            .Where(s => userClaims.Any(c => c.Type == s.Key)))
        {
            Claim existClaim = userClaims.First(c => c.Type == key);

            Task<IdentityResult> task = _userManager.ReplaceClaimAsync(user, existClaim, new(key, value)).ContinueWith<IdentityResult>(CatchSettingsSaveError);
            claimTasks.Add(task);
        }

        IEnumerable<IdentityResult> results = await Task.WhenAll(claimTasks).ConfigureAwait(false);
        if (results.Any(r => !r.Succeeded))
            throw new Exception("An occurred error happened while setting account settings.");

        // Refresh the sign in token (because some claims are saved inside the token)
        await _signInManager.RefreshSignInAsync(user);

        return Ok();
    }

    /// <summary>
    /// Validate setting keys and values
    /// </summary>
    /// <remarks>
    /// An ArgumentException is thrown when invalid data were found.
    /// </remarks>
    /// <param name="settings">The settings to validate</param>
    /// <exception cref="ArgumentException"></exception>
    [NonAction]
    private void ValidateSettings(Dictionary<string, string> settings)
    {
        IOptions<RequestLocalizationOptions> localizationOptions = HttpContext.RequestServices.GetService<IOptions<RequestLocalizationOptions>>()!;

        // Validate settings
        List<string> notFoundSettings = new();
        List<string> invalidSettings = new();
        foreach ((string key, string value) in settings)
        {
            switch (key)
            {
                case "culture":
                    IEnumerable<string> supportedUICultureString = localizationOptions.Value.SupportedUICultures!
                        .Select(c => c.Name);

                    if (!supportedUICultureString.Contains(value))
                        invalidSettings.Add(key);
                    break;
                case "theme":
                    if (!Enum.TryParse<Theme>(value, true, out _))
                        invalidSettings.Add(key);
                    break;
                default:
                    notFoundSettings.Add(key);
                    continue;
            }
        }

        // Return an error if any of the setting wasn't found
        if (notFoundSettings.Any())
        {
            string notFoundSettingsString = string.Join(", ", notFoundSettings);
            throw new ArgumentException(string.Format("The settings {0} aren't found.", notFoundSettingsString), nameof(settings));
        }

        // Return an error if any of the settings contains a invalid value
        if (invalidSettings.Any())
        {
            string invalidSettingsString = string.Join(", ", invalidSettings);
            throw new ArgumentException(string.Format("Invalid value for {0} was given.", invalidSettingsString), nameof(settings));
        }
    }

    /// <summary>
    /// If the result isn't successful the error would be logged
    /// </summary>
    /// <param name="task">The task of the result</param>
    [NonAction]
    private IdentityResult CatchSettingsSaveError(Task<IdentityResult> task)
    {
        // Error handling
        IdentityResult result = task.Result;
        if (!result.Succeeded)
        {
            string userId = _userManager.GetUserId(User)!;
            string errorJson = JsonConvert.SerializeObject(result.Errors);
            _logger.LogError("An identity error happened while setting account image of user {0}; error: {1}", userId, errorJson);
        }

        return result;
    }
}
