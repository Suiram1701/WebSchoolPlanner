using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace WebSchoolPlanner.Authorization;

/// <summary>
/// A requirement that validate if the login is 2fa confirmed
/// </summary>
public class MFAAuthorizationRequirement : AuthorizationHandler<MFAAuthorizationRequirement>, IAuthorizationRequirement
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MFAAuthorizationRequirement requirement)
    {
        HttpContext httpContext = (HttpContext)context.Resource!;
        Endpoint endpoint = httpContext.GetEndpoint()!;
        if (endpoint.Metadata.OfType<AllowAnonymousAttribute>().Any())     // No validation is required
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        Claim? enabledClaim = context.User.Claims.FirstOrDefault(c => c.Type == "mfa_enabled");
        Claim? amrClaim = context.User.Claims.FirstOrDefault(c => c.Type == "amr");

        if (enabledClaim?.Value == false.ToString())     // Not enabled
            context.Succeed(requirement);
        else if (amrClaim is null)     // Enabled but not confirmed
            context.Fail(new(requirement, "Login not 2FA confirmed"));
        else
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
