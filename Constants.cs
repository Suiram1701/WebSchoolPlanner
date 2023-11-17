using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace WebSchoolPlanner;

public static class Constants
{
    public const string ApiRoutePrefix = "api/v{version:apiVersion}/";

    #region ConfigurationPrefixes
    public const string AppInfoConfigurationPrefix = "ApplicationInfo:";

    public const string SwaggerConfigurationPrefix = "Swagger:";

    public const string JwtConfigurationPrefix = "Jwt:";

    public const string SigningKeyConfigurationPrefix = JwtConfigurationPrefix + "SigningKey:";
    #endregion

    #region Claims
    public const string ConfigClaimPrefix = "config_";
    #endregion

    public const long MaxAccountImageSize = 5000000;
}
