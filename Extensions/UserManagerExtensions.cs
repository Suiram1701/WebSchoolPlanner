using Microsoft.AspNetCore.Identity;
using OtpNet;
using System.Security.Claims;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.IdentityProviders;

namespace WebSchoolPlanner.Extensions;

public static class UserManagerExtensions
{
    private const string _twoFactorProvider = "TwoFactor";

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
        string provider = UserEmailTwoFactorTokenProvider<TUser>.ProviderName;
        return await userManager.GenerateUserTokenAsync(user, provider, _twoFactorProvider);
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
        UserEmailTwoFactorTokenProvider<TUser> provider = serviceProvider.GetService<UserEmailTwoFactorTokenProvider<TUser>>()!;
        return await provider.RemoveAsync(userManager, user, _twoFactorProvider);
    }

    /// <summary>
    /// Generates new two factor recovery codes for the specified user and invalidates the currently valid codes
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="userManager">The user manager to use</param>
    /// <param name="user">The use that owns the codes</param>
    /// <returns>The new generated codes</returns>
    public static async Task<IEnumerable<string>> GenerateTwoFactorRecoveryCodesAsync<TUser>(this UserManager<TUser> userManager, TUser user)
        where TUser : IdentityUser
    {
        string provider = UserTwoFactorRecoveryProvider<TUser>.ProviderName;
        string codes = await userManager.GenerateTwoFactorTokenAsync(user, provider);
        return codes.Split(';');
    }

    /// <summary>
    /// Removes all 2fa recovery codes
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="userManager">The user manager</param>
    /// <param name="user">The user</param>
    /// <param name="serviceProvider">The provider to access a required service</param>
    /// <returns>The succeed state</returns>
    public static async Task<IdentityResult> RemoveTwoFactorRecoveryCodesAsync<TUser>(this UserManager<TUser> userManager, TUser user, IServiceProvider serviceProvider)
        where TUser : IdentityUser
    {
        UserTwoFactorRecoveryProvider<TUser> provider = serviceProvider.GetService<UserTwoFactorRecoveryProvider<TUser>>()!;
        return await provider.RemoveAsync(userManager, user, _twoFactorProvider);
    }
}
