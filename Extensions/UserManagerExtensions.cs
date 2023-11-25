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
    public static async Task<IdentityResult> SetTwoFactorSecretAsync<TUser>(this UserManager<TUser> userManager, TUser user, byte[] secret)
        where TUser : IdentityUser
    {
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
}
