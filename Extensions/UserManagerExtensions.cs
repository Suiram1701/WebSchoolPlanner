using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using WebSchoolPlanner.Db.Models;

namespace WebSchoolPlanner.Extensions;

public static class UserManagerExtensions
{
    private const string _twoFactorClaim = SecurityClaimPrefix + "totp_TwoFactor";

    /// <summary>
    /// Sets the two factor claim value to a specified value
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="userManager">The user manager</param>
    /// <param name="user">The user</param>
    /// <param name="secret">The secret to set</param>
    /// <returns>The result</returns>
    public static async Task<IdentityResult> UpdateTwoFactorSecretAsync<TUser>(this UserManager<TUser> userManager, TUser user, byte[] secret)
        where TUser : IdentityUser
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        ArgumentNullException.ThrowIfNull(secret, nameof(secret));

        IList<Claim> userClaims = await userManager.GetClaimsAsync(user);
        Claim? existClaim = userClaims.FirstOrDefault(c => c.Type == _twoFactorClaim);
        if (existClaim is null)
        {
            Claim newClaim = new(_twoFactorClaim, Convert.ToHexString(secret));
            return await userManager.AddClaimAsync(user, newClaim);
        }
        else
        {
            Claim newClaim = new(_twoFactorClaim, Convert.ToHexString(secret));
            return await userManager.ReplaceClaimAsync(user, existClaim, newClaim);
        }
    }

    /// <summary>
    /// Removes the totp secret of the specified user
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="userManager"></param>
    /// <param name="user">The user</param>
    /// <returns>The result</returns>
    public static async Task<IdentityResult> RemoveTwoFactorSecretAsync<TUser>(this UserManager<TUser> userManager, TUser user)
        where TUser : IdentityUser
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));

        IList<Claim> claims = await userManager.GetClaimsAsync(user);
        Claim? secretClaim = claims.FirstOrDefault(c => c.Type == _twoFactorClaim);
        if (secretClaim is not null)
            return await userManager.RemoveClaimAsync(user, secretClaim);
        return IdentityResult.Success;
    }
}
