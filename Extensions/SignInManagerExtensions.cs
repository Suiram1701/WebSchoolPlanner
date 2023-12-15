using Microsoft.AspNetCore.Identity;
using WebSchoolPlanner.IdentityProviders;

namespace WebSchoolPlanner.Extensions;

public static class SignInManagerExtensions
{
    /// <summary>
    /// Do a two factor signs in via the specified method
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="signInManager">The sign in manager to use</param>
    /// <param name="method">The method to use</param>
    /// <param name="token">The code with them to sign in</param>
    /// <param name="rememberClient">The should the client be remembered for further sign ins</param>
    /// <returns>The result</returns>
    public static async Task<SignInResult> TwoFactorSignInAsync<TUser>(this SignInManager<TUser> signInManager, TwoFactorMethod method, string token, bool rememberClient)
        where TUser : IdentityUser
    {
        string provider = Helpers.DetermineProviderName<TUser>(method, signInManager.Logger);
        return await signInManager.TwoFactorSignInAsync(provider, token, false, rememberClient);
    }
}
