using Microsoft.AspNetCore.Authorization;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Reflection.Metadata;

namespace WebSchoolPlanner.Swagger;

/// <summary>
/// A general operation filter for swagger
/// </summary>
public class SwaggerOperationFilter : IOperationFilter
{
    private const string _mdnLinkTemplate = "https://developer.mozilla.org/en-US/docs/Web/HTTP/{0}/{1}";

    void IOperationFilter.Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Allow anonymous access
        if (context.MethodInfo.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            operation.Description += " This endpoint can be accessed anonymously.";

        // mdn link for request headers
        foreach (OpenApiParameter parameter in operation.Parameters     // Headers
            .Where(p => p.In == ParameterLocation.Header)
            .Where(p => IsDefaultHeaderName(p.Name)))
        {
            parameter.Description += BuildMdnLink("Headers", parameter.Name);
        }
    }

    /// <summary>
    /// The method checks whether the <see cref="HeaderNames"/> class contains the given header name.
    /// </summary>
    /// <param name="name">The header name</param>
    /// <returns>The result</returns>
    public static bool IsDefaultHeaderName(string name)
    {
        Type headerNamesType = typeof(HeaderNames);
        foreach (FieldInfo field in headerNamesType.GetFields(BindingFlags.Static | BindingFlags.Public))
        {
            string headerValue = (string)field.GetValue(null)!;
            if (name == headerValue)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Builds a help link includes text to the mdn web docs
    /// </summary>
    /// <param name="category">The category of the content. e.g. Headers</param>
    /// <param name="content">The content. e.g. Accept</param>
    /// <returns>The html links including text</returns>
    public static string BuildMdnLink(string category, string content)
    {
        string mdnLink = string.Format(_mdnLinkTemplate, category, content);
        string mdnLinkHtml = string.Format("<a href=\"{0}\" target=\"_blank\">mdn web docs</a>", mdnLink);
        return string.Format(" See in {0} for more use and format information.", mdnLinkHtml);
    }
}
