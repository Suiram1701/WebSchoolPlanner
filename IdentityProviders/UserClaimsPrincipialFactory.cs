using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace WebSchoolPlanner.IdentityProviders;

/// <summary>
/// A factory that provides support to add in the app used claims
/// </summary>
/// <typeparam name="TUser">The type of the user</typeparam>
/// <typeparam name="TRole">The type of the role</typeparam>
public class UserClaimsPrincipialFactory<TUser, TRole> : UserClaimsPrincipalFactory<TUser, TRole>
    where TUser : IdentityUser
    where TRole : IdentityRole
{
    public UserClaimsPrincipialFactory(UserManager<TUser> userManager, RoleManager<TRole> roleManager, IOptions<IdentityOptions> options) : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(TUser user)
    {
        ClaimsIdentity claims = await base.GenerateClaimsAsync(user);
        claims.AddClaim(new("mfa_enabled", user.TwoFactorEnabled.ToString()));
        return claims;
    }
}
