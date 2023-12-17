using Microsoft.AspNetCore.Identity;
using WebSchoolPlanner.Db.Models;
using WebSchoolPlanner.IdentityProviders;

namespace WebSchoolPlanner.Extensions;

public static class IdentityBuilderExtensions
{
    /// <summary>
    /// Adds all three two factor token providers to the identity builder
    /// </summary>
    /// <typeparam name="TUser">The type of the user</typeparam>
    /// <param name="builder">The builder</param>
    /// <returns>The builder pipeline</returns>
    public static IdentityBuilder AddTwoFactorTokenProviders<TUser>(this IdentityBuilder builder)
        where TUser : IdentityUser
    {
        // The provider names that finally used are specified in UserManagerExtensions an SignInManagerExtensions
        builder.AddTokenProvider<UserTwoFactorTokenProvider<TUser>>(UserTwoFactorTokenProvider<TUser>.ProviderName);
        builder.AddTokenProvider<UserEmailTwoFactorTokenProvider<TUser>>(UserEmailTwoFactorTokenProvider<TUser>.ProviderName);
        builder.AddTokenProvider<UserTwoFactorRecoveryProvider<TUser>>(UserTwoFactorRecoveryProvider<TUser>.ProviderName);     // The GenerateAsync(string, UserManager<TUser>, TUser) of this provider have to the seperate every single recovery code by a ';'
        return builder;
    }
}
