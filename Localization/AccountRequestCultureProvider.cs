using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using System.Security.Claims;
using WebSchoolPlanner.Db.Models;

namespace WebSchoolPlanner.Localization;

/// <summary>
/// A provider that determine the culture from the users settings
/// </summary>
public class AccountRequestCultureProvider : RequestCultureProvider
{
    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        UserManager<User> userManager = httpContext.RequestServices.GetService<UserManager<User>>()!;

        User? user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return NullProviderCultureResult.Result;

        Claim? cultureClaim = (await userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == ConfigClaimPrefix + "culture");
        if (cultureClaim is null)
            return new("en");

        return new(cultureClaim.Value);
    }
}
