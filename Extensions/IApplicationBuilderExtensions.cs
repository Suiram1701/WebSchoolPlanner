namespace WebSchoolPlanner.Extensions;

public static class IApplicationBuilderExtensions
{
    public static IApplicationBuilder UseLocalization(this IApplicationBuilder app, params string[] ignoredPaths)
    {
        if (ignoredPaths.Length == 0)
            app.UseRequestLocalization();
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
            }, builder => builder.UseRequestLocalization());
        }

        return app;
    }
}
