using Microsoft.AspNetCore.Mvc;

namespace WebSchoolPlanner;

public static class Constants
{
    public const string RoutePrefix = "{languageCode?}/";

    public const string ApiPrefix = "api/v{version:apiVersion}/";
}
