using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Options;
using WebSchoolPlanner.Options;
using System.Security.Cryptography;
using WebSchoolPlanner.IdentityProviders.Interfaces;

namespace WebSchoolPlanner.IdentityProviders;

public class UserTwoFactorRecoveryProvider<TUser> : IUserTwoFactorTokenProvider<TUser>, IRemoveableToken<TUser>, ICountableToken<TUser>
    where TUser : IdentityUser
{
    /// <summary>
    /// The name of this provider
    /// </summary>
    public const string ProviderName = $"[{nameof(UserTwoFactorRecoveryProvider<TUser>)}]";

    private readonly ILogger _logger;
    private readonly IPasswordHasher<TUser> _passwordHasher;
    private readonly IOptions<MfaRecoveryOptions> _optionsAccessor;

    public UserTwoFactorRecoveryProvider(ILogger<UserTwoFactorRecoveryProvider<TUser>> logger, IPasswordHasher<TUser> passwordHasher, IOptions<MfaRecoveryOptions> optionsAccessor)
    {
        _logger = logger;
        _passwordHasher = passwordHasher;
        _optionsAccessor = optionsAccessor;
    }

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user) =>
        Task.FromResult(manager.SupportsUserAuthenticationTokens);

    public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(purpose, nameof(purpose));
        ArgumentNullException.ThrowIfNull(manager, nameof(manager));
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        // Generate new codes
        List<string> codeList = new();
        int codeCount = _optionsAccessor.Value.CodeCount;
        for (int i = 0; i < codeCount; i++)
            codeList.Add(TokenHelpers.GenerateFormattedCode());
        DistinctCodes(ref codeList);

        JArray codes = new();
        foreach (string code in codeList)
        {
            string hashedCode = _passwordHasher.HashPassword(user, code);
            codes.Add(hashedCode);
        }

        string jsonContent = codes.ToString();

        IdentityResult result = await manager.SetAuthenticationTokenAsync(user, ProviderName, purpose, jsonContent);
        if (!result.Succeeded)
        {
            string jsonError = JsonConvert.SerializeObject(result.Errors);
            _logger.LogError("Unable to save two factor recovery codes for user {0}; Error: {1}", user.Id, jsonError);
            throw new Exception("Unable to save two factor recovery codes");
        }

        _logger.LogInformation("New 2fa recovery codes for user {0} generated and saved", user.Id);
        return string.Join(';', codeList);
    }

    public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(purpose, nameof(purpose));
        ArgumentNullException.ThrowIfNull(manager, nameof(manager));
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        JArray? codes = await GetCodesArrayAsync(purpose, manager, user);
        if (!(codes?.Any() ?? false))     // No codes available
            return false;

        JToken? code = codes.FirstOrDefault(rc =>
        {
            string hashedSavedCode = rc.Value<string>()
                ?? string.Empty;

            PasswordVerificationResult result = _passwordHasher.VerifyHashedPassword(user, hashedSavedCode, token);
            return result != PasswordVerificationResult.Failed;
        });
        if (code is not null)     // Specified code found
        {
            codes.Remove(code);
            IEnumerable<string> newCodes = codes
                .Select(rc => rc.Value<string>())
                .Where(rc => rc is not null)!;
            string jsonContent = JsonConvert.SerializeObject(newCodes);

            // Invalidate the valid code
            IdentityResult result = await manager.SetAuthenticationTokenAsync(user, ProviderName, purpose, jsonContent);
            if (!result.Succeeded)
            {
                string jsonError = JsonConvert.SerializeObject(result.Errors);
                _logger.LogError("Unable to invalidate used two factor recovery codes for user {0}; Error: {1}", user.Id, jsonError);
                throw new Exception("Unable to invalidate used two factor recovery codes");
            }

            return true;
        }

        return false;
    }

    public async Task<IdentityResult> RemoveAsync(UserManager<TUser> manager, TUser user, string purpose)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        IdentityResult result = await manager.RemoveAuthenticationTokenAsync(user, ProviderName, purpose);
        if (result.Succeeded)
            _logger.LogInformation("2fa recovery codes for user {0} removed", user.Id);
        else
        {
            string jsonContent = JsonConvert.SerializeObject(result.Errors);
            _logger.LogError("An error happened while removing 2fa recovery codes for user {0}", user.Id);
        }

        return result;
    }

    public async Task<int> CountAsync(UserManager<TUser> manager, TUser user, string purpose)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(purpose, nameof(purpose));
        ArgumentNullException.ThrowIfNull(manager, nameof(manager));
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        JArray? codes = await GetCodesArrayAsync(purpose, manager, user);
        return codes?.Count() ?? 0;
    }

    /// <summary>
    /// Get the saved recovery codes
    /// </summary>
    /// <param name="purpose">The pupose of the saved token</param>
    /// <param name="manager">The manager to use</param>
    /// <param name="user">The user that owns the codes</param>
    /// <returns>The code array. <see langword="null"/> means no codes are available</returns>
    private async Task<JArray?> GetCodesArrayAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(purpose, nameof(purpose));
        ArgumentNullException.ThrowIfNull(manager, nameof(manager));
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        string? jsonContent = await manager.GetAuthenticationTokenAsync(user, ProviderName, purpose);
        if (string.IsNullOrEmpty(jsonContent))     // No codes are set
            return null;
        return JArray.Parse(jsonContent);
    }

    /// <summary>
    /// Regenerate all duplicate codes in the list
    /// </summary>
    /// <param name="codes">The list</param>
    private void DistinctCodes(ref List<string> codes)
    {
        int count = codes.Count;
        codes = codes.Distinct().ToList();
        
        if (count != codes.Count)
        {
            codes.Add(TokenHelpers.GenerateFormattedCode());
            DistinctCodes(ref codes);
        }
    }
}
