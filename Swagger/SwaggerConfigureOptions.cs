using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebSchoolPlanner.Swagger;

public class SwaggerConfigureOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;
    private readonly IConfiguration _configuration;

    public SwaggerConfigureOptions(IApiVersionDescriptionProvider provider, IConfiguration configuration)
    {
        _provider = provider;
        _configuration = configuration;
    }

    void IConfigureOptions<SwaggerGenOptions>.Configure(SwaggerGenOptions options)
    {
        foreach (ApiVersionDescription description in _provider.ApiVersionDescriptions)
        {
            OpenApiInfo apiInfo = new()
            {
                Title = "WebSchoolPlanner",
                Description = "A school planner für communication and interaction between and students and teachers.",
                Version = description.ApiVersion.ToString(),
                License = new()
                {
                    Name = "Apache License 2.0",
                    Url = new Uri("https://www.apache.org/licenses/LICENSE-2.0")
                }
            };

            if (description.IsDeprecated)
                apiInfo.Description += $" This API version has been deprecated. Use version {_provider.ApiVersionDescriptions.MaxBy(d => d.ApiVersion)!.ApiVersion} instead.";

            // Add the contact details from the configuration if available
            Dictionary<string, string?> contactValues = new()
            {
                {
                    nameof(OpenApiContact.Name),
                    _configuration["Swagger:Contact:Name"]
                },
                {
                    nameof(OpenApiContact.Email),
                    _configuration["Swagger:Contact:Email"]
                },
                {
                    nameof(OpenApiContact.Url),
                    _configuration["Swagger:Contact:Url"]
                }
            };
            if (!contactValues.Values.All(string.IsNullOrEmpty))
            {
                apiInfo.Contact = new()
                {
                    Name = contactValues[nameof(OpenApiContact.Name)],
                    Email = contactValues[nameof(OpenApiContact.Email)],
                };

                // Check if a valid uri is given
                if (Uri.TryCreate(contactValues[nameof(OpenApiContact.Url)], new(), out Uri? result))
                    apiInfo.Contact.Url = result;
            }

            options.SwaggerDoc(description.GroupName, apiInfo);
        }

        // Include the xml comments
        string xmlFileName = typeof(Program).Assembly.GetName().Name + ".xml";
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName), true);
    }
}
