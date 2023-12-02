using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace WebSchoolPlanner;

public static class Constants
{
    public const string ApiRoutePrefix = "api/v{version:apiVersion}/";

    public const string AppInfoConfigurationPrefix = "ApplicationInfo:";

    public const string SwaggerConfigurationPrefix = "Swagger:";

    public const string TotpConfigurationPrefix = "Totp:";

    public const string ConfigClaimPrefix = "config_";

    public const string SecurityClaimPrefix = "security_";

    public const long MaxAccountImageSize = 5000000;
}
