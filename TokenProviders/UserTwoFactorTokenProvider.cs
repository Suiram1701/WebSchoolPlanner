using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OtpNet;
using System.Security.Claims;
using System.Security.Cryptography;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.Options;

namespace WebSchoolPlanner.TokenProviders;

/// <summary>
/// The default implementation of <see cref="IUserTwoFactorTokenProvider{TUser}"/> using <see cref="Totp"/>
/// </summary>
/// <typeparam name="TUser">The type of the user</typeparam>
public class UserTwoFactorTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser>
    where TUser : IdentityUser
{
    private const string _totpClaim = SecurityClaimPrefix + "totp";

    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly TotpAuthenticationOptions _options;

    public UserTwoFactorTokenProvider(ILogger<UserTwoFactorTokenProvider<User>> logger, IConfiguration configuration, IOptions<TotpAuthenticationOptions> options)
    {
        _logger = logger;
        _configuration = configuration;
        _options = options.Value;
    }

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user) =>
        Task.FromResult(true);

    public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        byte[] secret = RandomNumberGenerator.GetBytes(32);
        string secretString = Convert.ToHexString(secret);

        IdentityResult result;
        Claim? existClaim = (await manager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == _totpClaim);
        if (existClaim is null)
            result = await manager.AddClaimAsync(user, new(_totpClaim, secretString));
        else
        {
            Claim newClaim = new(_totpClaim, secretString);
            result = await manager.ReplaceClaimAsync(user, existClaim, newClaim);
        }

        // Log it if an error happend
        if (!result.Succeeded)
        {
            string errorJson = JsonConvert.SerializeObject(result);
            _logger.LogError("An identity error happened while generate a new TOTP token for user {0}; error: {1}", user.Id, errorJson);
            throw new Exception("An occurred error happened while generate a new TOTP token.");
        }

        _logger.LogInformation("New TOTP token generated for user {0}", user.Id);
        return secretString;
    }

    public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
    {
        // Get the totp secret
        Claim secretClaim = (await manager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == _totpClaim)
            ?? throw new InvalidOperationException("No TOTP secret for the user was found.");
        byte[] secret = Convert.FromHexString(secretClaim.Value);

        // Verify
        int timestep = _options.ValidTimeSpan.Seconds;
        Totp otp = new(secret, timestep, _options.Algorithm, _options.DigitsCount);
        return otp.VerifyTotp(token, out _);
    }
}
