using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OtpNet;
using System.Security.Claims;
using System.Security.Cryptography;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.Options;

namespace WebSchoolPlanner.IdentityProviders;

/// <summary>
/// The default implementation of <see cref="IUserTwoFactorTokenProvider{TUser}"/> using <see cref="Totp"/>
/// </summary>
/// <remarks>
/// This implementation don't create a claim to the user it only generates the token
/// </remarks>
/// <typeparam name="TUser">The type of the user</typeparam>
public class UserTwoFactorTokenProvider<TUser> : IUserTwoFactorTokenProvider<TUser>
    where TUser : IdentityUser
{
    private const string _totpClaim = SecurityClaimPrefix + "totp";

    private readonly IConfiguration _configuration;
    private readonly TotpAuthenticationOptions _options;

    public UserTwoFactorTokenProvider(IConfiguration configuration, IOptions<TotpAuthenticationOptions> options)
    {
        _configuration = configuration;
        _options = options.Value;
    }

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user) =>
        Task.FromResult(true);

    public Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
    {
        Span<byte> secret = stackalloc byte[32];
        RandomNumberGenerator.Fill(secret);
        string secretString = Convert.ToHexString(secret);
        return Task.FromResult(secretString);
    }

    public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
    {
        // Get the totp secret
        IList<Claim> userClaims = await manager.GetClaimsAsync(user);
        Claim secretClaim = userClaims.FirstOrDefault(c => c.Type == string.Format("{0}_{1}", _totpClaim, purpose))
            ?? throw new InvalidOperationException("No TOTP secret for the user was found.");
        byte[] secret = Convert.FromHexString(secretClaim.Value);

        // Verify
        int timestep = _options.ValidTimeSpan.Seconds;
        Totp otp = new(secret, timestep, OtpHashMode.Sha1, _options.DigitsCount);
        return otp.VerifyTotp(token, out _);
    }
}
