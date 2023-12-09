using Microsoft.AspNetCore.Identity;
using OtpNet;
using System.Security.Claims;
using WebSchoolPlanner.Db.Models;

namespace WebSchoolPlanner.Extensions;

public static class UserManagerExtensions
{
    /// <summary>
    /// Removes all 2fa recovery codes
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="userManager">The user manager</param>
    /// <param name="user">The user</param>
    /// <returns>The succeed state</returns>
    public static async Task<IdentityResult> RemoveTwoFactorRecoveryCodesAsync<TUser>(this UserManager<TUser> userManager, TUser user)
        where TUser : IdentityUser
    {
        object? resultObj = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 0);
        if (resultObj is null)     // An error happened
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = nameof(RemoveTwoFactorRecoveryCodesAsync),
                Description = "An error happened while removing 2fa recovery codes"
            });
        }
        return IdentityResult.Success;
    }
}
