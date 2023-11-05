using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace WebSchoolPlanner;

public static class Constants
{
    public const string ApiRoutePrefix = "api/v{version:apiVersion}/";

    public const string SwaggerConfigurationPrefix = "Swagger:";

    public const string AppInfoConfigurationPrefix = "ApplicationInfo:";

    public const string ConfigClaimPrefix = "config_";

    public const long MaxAccountImageSize = 5000000;
}
