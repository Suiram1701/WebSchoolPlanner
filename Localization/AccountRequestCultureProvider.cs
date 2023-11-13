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
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        SignInManager<User> signInManager = httpContext.RequestServices.GetService<SignInManager<User>>()!;
        if (!signInManager.IsSignedIn(httpContext.User))
            return NullProviderCultureResult;

        IList<Claim> userClaims = (List<Claim>)httpContext.Items["userClaims"]!;
        Claim? cultureClaim = userClaims.FirstOrDefault(c => c.Type == ConfigClaimPrefix + "culture");
        if (cultureClaim is null)
            return Task.FromResult<ProviderCultureResult?>(new("en"));

        return Task.FromResult<ProviderCultureResult?>(new(cultureClaim.Value));
    }
}
