using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace WebSchoolPlanner;

public static class Constants
{
    public const string ApiPrefix = "api/v{version:apiVersion}/";

    public const string SwaggerConfigurationPrefix = "Swagger:";

    public const string AppInfoConfigurationPrefix = "ApplicationInfo:";
}
