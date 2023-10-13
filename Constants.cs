using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace WebSchoolPlanner;

public static class Constants
{
    public const string LanguageRouteKey = "languageCode";

    public const string RoutePrefix = $"{{{LanguageRouteKey}:culture?}}/";

    public const string ApiPrefix = "api/v{version:apiVersion}/";
}
