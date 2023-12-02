using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace WebSchoolPlanner.Authorization;

/// <summary>
/// A default requirement that validate if the user is logged in and not expired
/// </summary>
public class DefaultAuthorizationRequirement<TUser> : AuthorizationHandler<DefaultAuthorizationRequirement<TUser>>, IAuthorizationRequirement
    where TUser : IdentityUser
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DefaultAuthorizationRequirement<TUser> requirement)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(requirement, nameof(requirement));
        if (context.Resource is not HttpContext httpContext)
            throw new ArgumentException(string.Format("An instance of {0} expected as resource.", nameof(HttpContext)), nameof(context.Resource));

        if (!(context.User.Identity?.IsAuthenticated ?? false))
        {
            await httpContext.ChallengeAsync();
            return;
        }

        // Expiration validation
        Claim? issuedAtClaim = context.User.FindFirst("iat");
        Claim? expiresAtClaim = context.User.FindFirst("exp");
        if (issuedAtClaim is not null && expiresAtClaim is not null)
        {
            uint issuedAtUnix = uint.Parse(issuedAtClaim.Value);
            DateTimeOffset issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedAtUnix);

            uint expiresAtUnix = uint.Parse(expiresAtClaim.Value);
            DateTimeOffset expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix);

            if (issuedAt > DateTimeOffset.UtcNow || expiresAt < DateTimeOffset.UtcNow)
            {
                await httpContext.ChallengeAsync();
                return;
            }
        }
        else
        {
            ILoggerFactory loggerFactory = httpContext.RequestServices.GetService<ILoggerFactory>()!;
            ILogger logger = loggerFactory.CreateLogger<DefaultAuthorizationRequirement<TUser>>();
            UserManager<TUser> userManager = httpContext.RequestServices.GetService<UserManager<TUser>>()!;

            logger.LogWarning("Security issue: No expiration time for a session of user {0} detected", userManager.GetUserId(context.User));
        }

        context.Succeed(requirement);
        return;
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
