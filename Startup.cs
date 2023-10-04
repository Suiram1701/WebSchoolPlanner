using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using WebSchoolPlanner.Extensions;

namespace WebSchoolPlanner;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        services.AddApiVersioning();
        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        if (_configuration["Swagger:Use"] == true.ToString())
            services.AddSwagger();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseEndpoints(options => options.MapControllers());

        app.UseApiVersioning();

        if (_configuration["Swagger:Use"] == true.ToString())
            app.UseSwagger();

        if (_configuration["Swagger:UseUI"] == true.ToString())
        {
            app.UseSwaggerUI(options =>
            {
                // Register all api versions
                IApiVersionDescriptionProvider provider = app.ApplicationServices.GetService<IApiVersionDescriptionProvider>()!;
                foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
                {
                    string url = $"/swagger/{description.GroupName}/swagger.json";
                    string name = description.GroupName.ToUpperInvariant();
                    options.SwaggerEndpoint(url, name);
                }
            });
        }
    }
}
