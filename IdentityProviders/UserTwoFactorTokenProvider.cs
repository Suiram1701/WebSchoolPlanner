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
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly TotpAuthenticationOptions _options;

    private const string _tokenProvider = $"[{nameof(UserTwoFactorTokenProvider<TUser>)}]";

    public UserTwoFactorTokenProvider(ILogger<UserTwoFactorTokenProvider<TUser>> logger, IConfiguration configuration, IOptions<TotpAuthenticationOptions> options)
    {
        _logger = logger;
        _configuration = configuration;
        _options = options.Value;
    }

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user) =>
        Task.FromResult(manager.SupportsUserTwoFactor && manager.SupportsUserAuthenticationTokens);

    public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        string base32Secret = manager.GenerateNewAuthenticatorKey();

        IdentityResult result = await manager.SetAuthenticationTokenAsync(user, _tokenProvider, purpose, base32Secret);
        if (!result.Succeeded)
        {
            string errorJson = JsonConvert.SerializeObject(result.Errors);
            _logger.LogError("An occurred error happened while generating 2fa token for user: {0}; Error: {2}", user.Id, errorJson);
            throw new Exception(string.Format("An occurred error happened while generating 2fa token"));
        }

        return base32Secret;
    }

    public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
    {
        string? base32Secret = await manager.GetAuthenticationTokenAsync(user, _tokenProvider, purpose);
        if (string.IsNullOrEmpty(base32Secret))     // No secret is available
            throw new InvalidOperationException("There isn't a totp secret set for the specified user.");
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
        return await manager.RemoveAuthenticationTokenAsync(user, _tokenProvider, purpose);
    }
}
