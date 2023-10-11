using Microsoft.AspNetCore.Localization;

namespace WebSchoolPlanner.Extensions;

public static class IRequestLocalization
{
    /// <summary>
    /// Check if the path is localizable
    /// </summary>
    /// <param name="_"></param>
    /// <param name="httpContext">The context to check</param>
    /// <returns><see langword="true"/> when it's localizable</returns>
    public static bool IsLocalizablePath(this IRequestCultureProvider _, HttpContext httpContext)
    {
        string path = httpContext.Request.Path;
        bool invResult =
            path.StartsWith("/api") ||
            path.StartsWith("/swagger");

        return !invResult;
    }
}
