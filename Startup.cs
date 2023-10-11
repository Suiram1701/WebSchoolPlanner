using Humanizer;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Globalization;
using WebSchoolPlanner.Extensions;
using Microsoft.AspNetCore.Mvc.Razor;
using WebSchoolPlanner.Localization;
using Microsoft.AspNetCore.Mvc;

namespace WebSchoolPlanner;

public class Startup
{
    private const string _localizationPath = "Localization";

    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // MVC
        services
            .AddControllersWithViews()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix, options => options.ResourcesPath = _localizationPath);

        // Localization
        services
            .AddLocalization(options => options.ResourcesPath = _localizationPath)
            .AddRequestLocalization(options =>
            {
                List<CultureInfo> uiCultures = new()
                {
                    new("en"),
                    new("en-US"),
                    new("en-GB"),
                    new("es"),
                    new("es-ES"),
                    new("es-MX"),
                    new("de"),
                    new("de-DE"),
                    new("de-AT"),
                    new("fr"),
                    new("fr-FR"),
                    new("fr-BE"),
                    new("ru"),
                    new("ru-RU"),
                    new("ru-UA"),
                };
                IEnumerable<string> cultures = uiCultures
                    .Where(c => c.IsNeutralCulture || c.Name == "fr-BE")     // Use only neutral cultures except of fr-BE because it is supported
                    .Select(c => c.Name);

                // Set cultures
                options.SetDefaultCulture(uiCultures[0].Name);
                options.AddSupportedUICultures(uiCultures.Select(c => c.Name).ToArray());
                options.AddSupportedCultures(cultures.ToArray());

                // Other options
                options.FallBackToParentUICultures = true;
                options.FallBackToParentCultures = true;
                options.ApplyCurrentCultureToResponseHeaders = true;

                // Providers
                options.RequestCultureProviders = new List<Microsoft.AspNetCore.Localization.IRequestCultureProvider>()
                {
                    new RouteValueCultureProvider { Options = options },
                    new HeaderValueCultureProvider { Options = options }
                };
            });

        // Api / swagger
        services
            .AddApiVersioning()
            .AddVersionedApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        if (_configuration["Swagger:Use"] == true.ToString())
            services.AddSwagger();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Error handling
        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();
        else
            app.UseHsts();

        // Default request pipeline
        app
            .UseHttpsRedirection()
            .UseStaticFiles();

        // Routing
        app
            .UseRouting()
            .UseRequestLocalization()
            .UseEndpoints(endpoints => endpoints.MapControllers());

        // Api / Swagger
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
