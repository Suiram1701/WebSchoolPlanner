using Microsoft.AspNetCore.Identity;
using WebSchoolPlanner.IdentityProviders;

namespace WebSchoolPlanner.Extensions;

/// <summary>
/// Some helpers for the extensions
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Determines the two factor provider name to use
    /// </summary>
    /// <param name="method">The requested two factor method</param>
    /// <param name="logger">The logger to log errors if necessary</param>
    /// <returns>The result</returns>
    public static string DetermineProviderName<TUser>(TwoFactorMethod method, ILogger logger)
        where TUser : IdentityUser
    {
        string provider = method switch
        {
            TwoFactorMethod.App => UserTwoFactorTokenProvider<TUser>.ProviderName,
            TwoFactorMethod.Email => UserEmailTwoFactorTokenProvider<TUser>.ProviderName,
            TwoFactorMethod.Recovery => UserTwoFactorRecoveryProvider<TUser>.ProviderName,
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(provider))
        {
            string message = string.Format("The requested two factor sign in method is not implemented by the method; Method: {0}", method);
            logger.LogError(message);
            throw new NotSupportedException(message);
        }

        return provider;
    }
}
