using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using WebSchoolPlanner.Authorization.Attributes;

namespace WebSchoolPlanner.Authorization;

/// <summary>
/// A requirement that validate if the login is 2fa confirmed
/// </summary>
public class MFAAuthorizationRequirement : AuthorizationHandler<MFAAuthorizationRequirement>, IAuthorizationRequirement
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MFAAuthorizationRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(requirement, nameof(requirement));
        if (context.Resource is not HttpContext httpContext)
            throw new ArgumentException(string.Format("An instance of {0} expected as resource.", nameof(HttpContext)), nameof(context.Resource));

        Endpoint endpoint = httpContext.GetEndpoint()!;
        if (endpoint.Metadata.Any(m => m.GetType() == typeof(AllowWithoutMfaAttribute)))     // No validation is required
        {
            context.Succeed(requirement);
            return;
        }

        bool result = Handle(httpContext);
        if (result)
            context.Succeed(requirement);
        else
        {
            context.Fail(new(requirement, "Login not 2FA confirmed"));
            
            // Redirect only if authenticated but not confirmed
            if (context.User.Identity?.IsAuthenticated ?? false)
                await Do2faRedirectAsync(httpContext);
        }

        return;
    }

    /// <summary>
    /// Handle the requirement
    /// </summary>
    /// <remarks>
    /// This won't consider <see cref="AllowWithoutMfaAttribute"/> exceptions 
    /// </remarks>
    /// <param name="httpContext">The httpContext</param>
    /// <returns>Is it failed or not</returns>
    public static bool Handle(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext, nameof(httpContext));

        Claim? enabledClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "mfa_enabled");
        Claim? amrClaim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "amr");

        if (enabledClaim?.Value == false.ToString())     // Not enabled
            return true;
        else if (amrClaim is null)     // Enabled but not confirmed
            return false;
        else
            return true;
    }

    /// <summary>
    /// Redirect the request to the 2fa action
    /// </summary>
    /// <remarks>
    /// Note: this method abort the request
    /// </remarks>
    /// <param name="httpContext">The httpContext</param>
    private async Task Do2faRedirectAsync(HttpContext httpContext)
    {
        string path = httpContext.Request.Path;
        string encodedPath = UrlEncoder.Default.Encode(path); 
        string targetPath = "/auth/2fa?r=" + encodedPath;
        httpContext.Response.Redirect(targetPath, false, false);

        await httpContext.Response.StartAsync(CancellationToken.None);
        httpContext.Abort();
    }
}
