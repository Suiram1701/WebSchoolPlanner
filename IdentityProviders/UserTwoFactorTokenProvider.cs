using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OtpNet;
using System.Security.Claims;
using System.Security.Cryptography;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.Options;
using static QRCoder.PayloadGenerator;
using static QRCoder.PayloadGenerator.OneTimePassword;

namespace WebSchoolPlanner.IdentityProviders;

/// <summary>
/// The default implementation of <see cref="IUserTwoFactorTokenProvider{TUser}"/> using <see cref="Totp"/>
/// </summary>
/// <typeparam name="TUser">The type of the user</typeparam>
public class UserTwoFactorTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser>
    where TUser : IdentityUser
{
    /// <summary>
    /// The name of this provider
    /// </summary>
    public const string ProviderName = $"[{nameof(UserTwoFactorTokenProvider<TUser>)}]";

    private readonly ILogger _logger;
    private readonly IDataProtectionProvider _dataProtectorProvider;
    private readonly TotpAuthenticationOptions _options;

    public UserTwoFactorTokenProvider(ILogger<UserTwoFactorTokenProvider<TUser>> logger, IDataProtectionProvider dataProtectorProvider, IOptions<TotpAuthenticationOptions> options)
    {
        _logger = logger;
        _dataProtectorProvider = dataProtectorProvider;
        _options = options.Value;
    }

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user) =>
        Task.FromResult(manager.SupportsUserTwoFactor && manager.SupportsUserAuthenticationTokens);

    public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        // Create a token and protect it
        string base32Secret = manager.GenerateNewAuthenticatorKey();
        string protectedSecret = TokenHelpers.ProtectToken(base32Secret, _dataProtectorProvider.CreateProtector(purpose));

        // Save it
        IdentityResult result = await manager.SetAuthenticationTokenAsync(user, ProviderName, purpose, protectedSecret);
        if (!result.Succeeded)
        {
            string errorJson = JsonConvert.SerializeObject(result.Errors);
            _logger.LogError("An occurred error happened while generating 2fa token for user: {0}; Error: {2}", user.Id, errorJson);
            throw new Exception(string.Format("An occurred error happened while generating 2fa token"));
        }

        _logger.LogInformation("New 2fa secret for user {0} generated and saved", user.Id);
        return base32Secret;
    }

    public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
    {
        // Read an unprotect it
        string? protectedSecret = await manager.GetAuthenticationTokenAsync(user, ProviderName, purpose);
        if (string.IsNullOrEmpty(protectedSecret))     // No secret is available
            throw new InvalidOperationException("There isn't a totp secret set for the specified user.");

        string base32Secret = TokenHelpers.UnprotectToken(protectedSecret, _dataProtectorProvider.CreateProtector(purpose));
        byte[] secret = Base32Encoding.ToBytes(base32Secret);

        // Verify
        int timestep = _options.ValidTimeSpan.Seconds;
        Totp otp = new(secret, timestep, OtpHashMode.Sha1, _options.DigitsCount);
        return otp.VerifyTotp(token, out _);
    }

    /// <summary>
    /// Removes the token with the specified purpose
    /// </summary>
    /// <param name="manager">The manager to use</param>
    /// <param name="user">The user</param>
    /// <param name="purpose">The name of the token</param>
    /// <returns>The result state</returns>
    public async Task<IdentityResult> RemoveAsync(UserManager<TUser> manager, TUser user, string purpose)
    {
        IdentityResult result = await manager.RemoveAuthenticationTokenAsync(user, ProviderName, purpose);
        if (result.Succeeded)
            _logger.LogInformation("2fa secret for use {0} removed", user.Id);
        else
        {
            string jsonError = JsonConvert.SerializeObject(result.Errors);
            _logger.LogError("An error happened while removing 2fa secret for user {0}; Error: {1}", user.Id, jsonError);
        }

        return result;
    }
}
