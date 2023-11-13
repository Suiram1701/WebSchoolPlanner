using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using WebSchoolPlanner.Db.Models;

namespace WebSchoolPlanner.Middlewares;

/// <summary>
/// A middleware that takes default database requests
/// </summary>
public class DatabaseRequestMiddleware
{
    private readonly RequestDelegate _next;

    public DatabaseRequestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        SignInManager<User> signInManager = context.RequestServices.GetService<SignInManager<User>>()!;
        UserManager<User> userManager = context.RequestServices.GetService<UserManager<User>>()!;
        
        if (signInManager.IsSignedIn(context.User))
        {
            User user = (await userManager.GetUserAsync(context.User))!;
            context.Items["user"] = user;

            IList<Claim> userClaims = await userManager.GetClaimsAsync(user);
            context.Items["userClaims"] = userClaims;
        }

        await _next(context);
    }
}
