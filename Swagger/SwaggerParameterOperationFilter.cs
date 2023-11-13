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
        // Optional parameters
        foreach (OpenApiParameter parameter in operation.Parameters.Where(p => !p.Required))
        {
            parameter.Description += " OPTIONAL";

            // Get the parameter info from the action
            ParameterInfo[] parameters = context.MethodInfo.GetParameters();
            ParameterInfo? parameterInfo = parameters.FirstOrDefault(p => p.GetCustomAttributes<Attribute>().Select(a => a as IModelNameProvider).Any(mnp => mnp?.Name == parameter.Name));
            parameterInfo ??= parameters.First(p => p.Name == parameter.Name);

            if (parameterInfo.HasDefaultValue)
            {
                string? defaultValue = parameterInfo.DefaultValue?.ToString();
                
                if (defaultValue is not null)
                    parameter.Description += (". Is by default set to " + defaultValue);
            }
        }
    }
}
