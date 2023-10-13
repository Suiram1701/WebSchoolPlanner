namespace WebSchoolPlanner.Extensions;

public static class IApplicationBuilderExtensions
{
    public static IApplicationBuilder UseLocalization(this IApplicationBuilder app, params string[] ignoredPaths)
    {
        if (ignoredPaths.Length == 0)
        {
            app.UseRequestLocalization();
            app.Use(LocalizationRedirectionMiddleware);
        }
        else
        {
            app.UseWhen(httpContext =>
            {
                // When the current request path starts with any of the ignored paths than don't use localization
                foreach (string path in ignoredPaths)
                {
                    if (httpContext.Request.Path.Value!.StartsWith(path))
                        return false;
                }
                return true;
            }, builder =>
            {
                builder.UseRequestLocalization();
                builder.Use(LocalizationRedirectionMiddleware);
            });
        }

        return app;
    }

    private static Task LocalizationRedirectionMiddleware(HttpContext httpContext, RequestDelegate next)
    {
        string? languageCode = httpContext.GetRouteValue(LanguageRouteKey)?.ToString();

        string currentCultureString = Thread.CurrentThread.CurrentUICulture.Name;
        string newPath;

        if (languageCode == currentCultureString)     // Continue the request pipeline when culture equals requested culture
            return next(httpContext);

        // Determine the new path
        if (languageCode is null)
            newPath = '/' + currentCultureString + httpContext.Request.Path.Value;
        else
            newPath = httpContext.Request.Path.Value!.Replace(currentCultureString, languageCode);

        httpContext.Response.Redirect(newPath);
        return Task.CompletedTask;
    }
}
