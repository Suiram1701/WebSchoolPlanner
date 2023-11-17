using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace WebSchoolPlanner.Swagger;

/// <summary>
/// An operation filter that process parameters
/// </summary>
public class SwaggerParameterOperationFilter : IOperationFilter
{
    void IOperationFilter.Apply(OpenApiOperation operation, OperationFilterContext context)
    {
    }
}
