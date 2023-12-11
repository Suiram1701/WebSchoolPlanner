using Microsoft.AspNetCore.Identity;
using OtpNet;
using System.Security.Claims;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.IdentityProviders;

namespace WebSchoolPlanner.Extensions;

public static class UserManagerExtensions
{
    private const string _emailTwoFactorPurpose = "EmailTwoFactor";

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
            userManager.Logger.LogError("An error happened while removing recovery codes for user {0}", user.Id);
            return IdentityResult.Failed(new IdentityError
            {
                Code = nameof(RemoveTwoFactorRecoveryCodesAsync),
                Description = "An error happened while removing 2fa recovery codes"
            });
        }
        return IdentityResult.Success;
    }

    /// <summary>
    /// Generates an two factor code for email confirmation
    /// </summary>
    /// <remarks>
    /// This won't send the email
    /// </remarks>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="userManager">The user manager</param>
    /// <param name="user">The user</param>
    /// <returns>The created code</returns>
    public static async Task<string> GenerateEmailTwoFactorTokenAsync<TUser>(this UserManager<TUser> userManager, TUser user)
        where TUser : IdentityUser
    {
        return await userManager.GenerateUserTokenAsync(user, UserEmailTwoFactorTokenProvider<TUser>.ProviderName, _emailTwoFactorPurpose);
    }

    /// <summary>
    /// Removes any email two factor codes of the specified user
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="userManager">The user manager</param>
    /// <param name="user">The user</param>
    /// <param name="serviceProvider">The provider to access a required service</param>
    /// <returns>The result</returns>
    public static async Task<IdentityResult> RemoveEmailTwoFactorTokenAsync<TUser>(this UserManager<TUser> userManager, TUser user, IServiceProvider serviceProvider)
        where TUser : IdentityUser
    {
        UserEmailTwoFactorTokenProvider<TUser> emailTokenProvider = serviceProvider.GetService<UserEmailTwoFactorTokenProvider<TUser>>()!;
        return await emailTokenProvider.RemoveAsync(userManager, user, _emailTwoFactorPurpose);
    }
}
