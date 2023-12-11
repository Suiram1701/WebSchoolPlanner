using Microsoft.AspNetCore.Identity;
using WebSchoolPlanner.IdentityProviders;

namespace WebSchoolPlanner.Extensions;

public static class SignInManagerExtensions
{
    /// <summary>
    /// Signs a user with an email two factor code in
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="signInManager">The sign in manager</param>
    /// <param name="code">The code to validate</param>
    /// <param name="rememberClient">Should the browser be remembered</param>
    /// <returns>The result of the sign in</returns>
    public static Task<SignInResult> EmailTwoFactorSignInAsync<TUser>(this SignInManager<TUser> signInManager, string code, bool rememberClient)
        where TUser : IdentityUser
    {
        return signInManager.TwoFactorSignInAsync(UserEmailTwoFactorTokenProvider<TUser>.ProviderName, code, false, rememberClient);
    }
}
