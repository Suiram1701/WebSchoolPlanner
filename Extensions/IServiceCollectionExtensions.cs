using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using WebSchoolPlanner.Swagger;

namespace WebSchoolPlanner.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        return services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerConfigureOptions>()
            .AddSwaggerGen(options => options.OperationFilter<SwaggerResponseOperationFilter>());
    }
}
