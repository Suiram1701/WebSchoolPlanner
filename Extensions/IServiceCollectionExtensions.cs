using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using WebSchoolPlanner.Swagger;

namespace WebSchoolPlanner.Extensions;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Add swagger doc generation with all option filters and configure options
    /// </summary>
    /// <param name="services"></param>
    /// <returns>The services</returns>
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        return services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerConfigureOptions>()
            .AddSwaggerGen(options =>
            {
                options.OperationFilter<SwaggerOperationFilter>();
                options.OperationFilter<SwaggerParameterOperationFilter>();
                options.OperationFilter<SwaggerResponseOperationFilter>();
            });
    }
}
