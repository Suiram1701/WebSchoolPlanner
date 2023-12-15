using Microsoft.AspNetCore.Identity;
using OtpNet;
using System.Security.Claims;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.IdentityProviders;

namespace WebSchoolPlanner.Extensions;

public static class UserManagerExtensions
{
    /// <summary>
    /// Removes the teo factor app secret of the specified user
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="userManager">The user manager to use</param>
    /// <param name="user">The user that owns the secret</param>
    /// <param name="serviceProvider">The provider to access a required service</param>
    /// <returns>The result</returns>
    public static async Task<IdentityResult> RemoveTwoFactorSecretAsync<TUser>(this UserManager<TUser> userManager, TUser user, IServiceProvider serviceProvider)
        where TUser : IdentityUser
    {
        UserTwoFactorTokenProvider<TUser> provider = serviceProvider.GetService<UserTwoFactorTokenProvider<TUser>>()!;
        string purpose = Helpers.GetTwoFactorPurpose(TwoFactorMethod.App);
        return await provider.RemoveAsync(userManager, user, purpose);
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
        string provider = UserEmailTwoFactorTokenProvider<TUser>.ProviderName;
        string purpose = Helpers.GetTwoFactorPurpose(TwoFactorMethod.Email);
        return await userManager.GenerateUserTokenAsync(user, provider, purpose);
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
        string purpose = Helpers.GetTwoFactorPurpose(TwoFactorMethod.Email);
        return await provider.RemoveAsync(userManager, user, purpose);
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
        string purpose = Helpers.GetTwoFactorPurpose(TwoFactorMethod.Recovery);
        string codes = await userManager.GenerateUserTokenAsync(user, provider, purpose);
        return codes.Split(';');
    }

    /// <summary>
    /// Get the count of all valid two factor recovery codes
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="userManager">The user manager to use</param>
    /// <param name="user">The user that owns the codes</param>
    /// <param name="serviceProvider">The provider to access a required service</param>
    /// <returns>The count of the valid codes</returns>
    public static async Task<int> CountTwoFactorRecoveryCodesAsync<TUser>(this UserManager<TUser> userManager, TUser user, IServiceProvider serviceProvider)
        where TUser : IdentityUser
    {
        UserTwoFactorRecoveryProvider<TUser> provider = serviceProvider.GetService<UserTwoFactorRecoveryProvider<TUser>>()!;
        string purpose = Helpers.GetTwoFactorPurpose(TwoFactorMethod.Recovery);
        return await provider.CountCodesAsync(userManager, user, purpose);
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
        string purpose = Helpers.GetTwoFactorPurpose(TwoFactorMethod.Recovery);
        return await provider.RemoveAsync(userManager, user, purpose);
    }

    public static async Task<bool> VerifyTwoFactorAsync<TUser>(this UserManager<TUser> userManager, TUser user, TwoFactorMethod method, string token)
        where TUser : IdentityUser
    {
        string provider = Helpers.DetermineProviderName<TUser>(method, userManager.Logger);
        string purpose = Helpers.GetTwoFactorPurpose(method);
        return await userManager.VerifyUserTokenAsync(user, provider, purpose, token);
    }
}
