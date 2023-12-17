using Microsoft.AspNetCore.Identity;
using OtpNet;
using System.Security.Claims;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.IdentityProviders;
using System.Reflection;
using WebSchoolPlanner.IdentityProviders.Interfaces;

namespace WebSchoolPlanner.Extensions;

public static class UserManagerExtensions
{
    private const string _twoFactorPurpose = "TwoFactor";

    /// <summary>
    /// Returns the private _tokenProviders field of <see cref="UserManager{TUser}"/>
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="instance">The instance to use</param>
    /// <returns>The result</returns>
    private static Dictionary<string, IUserTwoFactorTokenProvider<TUser>> GetTokenProviders<TUser>(UserManager<TUser> instance)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(instance, nameof(instance));

        Type type = typeof(UserManager<>).MakeGenericType(typeof(TUser));
        FieldInfo info = type.GetField("_tokenProviders", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Dictionary<string, IUserTwoFactorTokenProvider<TUser>>)info.GetValue(instance)!;
    }

    /// <summary>
    /// Returns the a removeable provider instance for the specified provider name
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="userManager">The user manager to use</param>
    /// <param name="providerName">The name of the provider</param>
    /// <returns>A instance of the provider</returns>
    private static IRemoveableToken<TUser> GetRemoveableProvider<TUser>(UserManager<TUser> userManager, string providerName)
        where TUser : IdentityUser
    {
        IRemoveableToken<TUser>? provider = GetTokenProviders<TUser>(userManager)[providerName] as IRemoveableToken<TUser>;
        if (provider is null)
            throw new NotSupportedException("The specified provider won't support the removing or the provider is generally not supported by the UserManager<TUser>.");
        return provider;
    }

    /// <summary>
    /// Removes the teo factor app secret of the specified user
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="userManager">The user manager to use</param>
    /// <param name="user">The user that owns the secret</param>
    /// <returns>The result</returns>
    public static async Task<IdentityResult> RemoveTwoFactorSecretAsync<TUser>(this UserManager<TUser> userManager, TUser user)
        where TUser : IdentityUser
    {
        string providerName = UserTwoFactorTokenProvider<TUser>.ProviderName;
        return await GetRemoveableProvider<TUser>(userManager, providerName).RemoveAsync(userManager, user, _twoFactorPurpose);
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
        return await userManager.GenerateTwoFactorTokenAsync(user, provider);
    }

    /// <summary>
    /// Removes any email two factor codes of the specified user
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="userManager">The user manager</param>
    /// <param name="user">The user</param>
    /// <returns>The result</returns>
    public static async Task<IdentityResult> RemoveEmailTwoFactorTokenAsync<TUser>(this UserManager<TUser> userManager, TUser user)
        where TUser : IdentityUser
    {
        string providerName = UserEmailTwoFactorTokenProvider<TUser>.ProviderName;
        return await GetRemoveableProvider<TUser>(userManager, providerName).RemoveAsync(userManager, user, _twoFactorPurpose);
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
    /// Get the count of all valid two factor recovery codes
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="userManager">The user manager to use</param>
    /// <param name="user">The user that owns the codes</param>
    /// <returns>The count of the valid codes</returns>
    public static async Task<int> CountTwoFactorRecoveryCodesAsync<TUser>(this UserManager<TUser> userManager, TUser user)
        where TUser : IdentityUser
    {
        string providerName = UserTwoFactorRecoveryProvider<TUser>.ProviderName;
        ICountableToken<TUser>? provider = GetTokenProviders<TUser>(userManager)[providerName] as ICountableToken<TUser>;
        if (provider is null)
            throw new NotSupportedException("The specified provider won't support counting or the provider is generally not supported by the UserManager<TUser>.");

        return await provider.CountAsync(userManager, user, _twoFactorPurpose);
    }

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
        string providerName = UserTwoFactorRecoveryProvider<TUser>.ProviderName;
        return await GetRemoveableProvider<TUser>(userManager, providerName).RemoveAsync(userManager, user, _twoFactorPurpose);
    }

    public static async Task<bool> VerifyTwoFactorAsync<TUser>(this UserManager<TUser> userManager, TUser user, TwoFactorMethod method, string token)
        where TUser : IdentityUser
    {
        string provider = Helpers.DetermineProviderName<TUser>(method, userManager.Logger);
        return await userManager.VerifyTwoFactorTokenAsync(user, provider, token);
    }

    /// <summary>
    /// Set the email two factor value of the user
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="userManager">The user manager to use</param>
    /// <param name="user">The user</param>
    /// <param name="enabled">The value to set</param>
    /// <returns>The result</returns>
    public static async Task<IdentityResult> SetEmailTwoFactorEnabledAsync<TUser>(this UserManager<TUser> userManager, TUser user, bool enabled)
        where TUser : User
    {
        user.IsEmailTwoFactorEnabled = enabled;
        return await userManager.UpdateAsync(user);
    }

    /// <summary>
    /// Set the last login date time to the specified value
    /// </summary>
    /// <param name="userManager">The user manager</param>
    /// <param name="user">The user that have the specified time</param>
    /// <param name="dateTime">The date time</param>
    /// <returns>The result</returns>
    public static async Task<IdentityResult> SetLastLoginAsync(this UserManager<User> userManager, User user, DateTime dateTime)
    {
        ArgumentNullException.ThrowIfNull(userManager, nameof(userManager));
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        ArgumentNullException.ThrowIfNull(dateTime, nameof(dateTime));

        user.LastLogin = dateTime;
        return await userManager.UpdateAsync(user);
    }
}
