using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using WebSchoolPlanner.Swagger.Attributes;

namespace WebSchoolPlanner.Swagger;

/// <summary>
/// The default operation filter for swagger that adds error responses
/// </summary>
public class SwaggerResponseOperationFilter : IOperationFilter
{
    private const string _jsonResponse = "application/json";
    private const string _jsonProblemResponse = "application/problem+json";

#pragma warning disable CS8618
    private OpenApiSchema _problemSchemaRef;
    private OpenApiSchema _exceptionSchemaRef;
#pragma warning restore CS8618

    void IOperationFilter.Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!context.SchemaRepository.Schemas.ContainsKey("ProblemErrorResponse"))
            SetupSchemas(context);

        // Authentication
        if (context.MethodInfo.GetCustomAttribute<AllowAnonymousAttribute>() is null)
        {
            operation.Responses.Add(StatusCodes.Status401Unauthorized.ToString(), new()
            {
                Description = "The authentication information is missing or is invalid.",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    {
                        _jsonProblemResponse,
                        new() { Schema = _problemSchemaRef }
                    }
                }
            });

            operation.Responses.Add(StatusCodes.Status403Forbidden.ToString(), new()
            {
                Description = "The logged in account isn't authorized to call this method.",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    {
                        _jsonProblemResponse,
                        new() { Schema = _problemSchemaRef }
                    }
                }
            });
        }

        // Parameters
        if (operation.Parameters.Any() || (operation.RequestBody?.Required ?? false))
        {
            operation.Responses.Add(StatusCodes.Status400BadRequest.ToString(), new()
            {
                Description = "The request contains invalid data.",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    {
                        _jsonResponse,
                        new() { Schema = _exceptionSchemaRef }
                    },
                    {
                        _jsonProblemResponse,
                        new() { Schema = _problemSchemaRef }
                    }
                }
            });

            // Not acceptable
            if (operation.Parameters.Any(p => p.In == ParameterLocation.Header &&
                (p.Name == HeaderNames.Accept
                || p.Name == HeaderNames.AcceptEncoding
                || p.Name == HeaderNames.AcceptLanguage)))
            {
                operation.Responses.Add(StatusCodes.Status406NotAcceptable.ToString(), new()
                {
                    Description = "The content of the 'Accept', 'Accept-Language' or 'Accept-Range' header is invalid.",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            _jsonProblemResponse,
                            new() { Schema = _problemSchemaRef }
                        }
                    }
                });
            }
        }

        if (operation.RequestBody?.Required ?? false)
        {
            operation.Responses.Add(StatusCodes.Status415UnsupportedMediaType.ToString(), new()
            {
                Description = "The Content-Type of the request isn't supported by the method.",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    {
                        _jsonProblemResponse,
                        new() { Schema = _problemSchemaRef }
                    }
                }
            });
        }

        // Response headers
        foreach (ProducesResponseHeaderAttribute attribute in context.MethodInfo.GetCustomAttributes<ProducesResponseHeaderAttribute>())
        {
            OpenApiSchema schema = context.SchemaGenerator.GenerateSchema(attribute.HeaderType, context.SchemaRepository);
            OpenApiHeader header = new()
            {
                Description = attribute.Description,
                Schema = schema
            };

            if (attribute.StatusCodes.Any(sc => sc == -1))
            {
                foreach ((string _, OpenApiResponse response) in operation.Responses)
                    response.Headers.Add(attribute.HeaderName, header);
            }
            else
            {
                foreach ((string key, OpenApiResponse response) in operation.Responses)
                {
                    if (attribute.StatusCodes.Any(sc => sc.ToString() == key))
                        response.Headers.Add(attribute.HeaderName, header);
                }
            }
        }

        // Internal server error
        operation.Responses.Add("5XX", new()
        {
            Description = "An internal error happend while the request.",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                {
                    _jsonResponse,
                    new() { Schema = _exceptionSchemaRef }
                }
            }
        });

        // mdn link for response headers
        foreach ((string _, OpenApiResponse response) in operation.Responses)
        {
            foreach ((string name, OpenApiHeader header) in response.Headers.Where(h => SwaggerOperationFilter.IsDefaultHeaderName(h.Key)))
                header.Description += SwaggerOperationFilter.BuildMdnLink("Headers", name);
        }
    }

    private void SetupSchemas(OperationFilterContext context)
    {
        const string problemSchemaId = "ProblemErrorResponse";
        _problemSchemaRef = context.SchemaRepository.AddDefinition(problemSchemaId, new()
        {
            Title = "Problem response",
            Description = "A response that represent a problem in the request.",
            Example = OpenApiAnyFactory.CreateFromJson(
                """
                {
                    "type": "Details of the problem",
                    "title": "Title of the problem",
                    "status": 400,
                    "traceId": "",
                    "errors": { }
                }
                """)
        });
        context.SchemaRepository.RegisterType(typeof(ProblemDetails), problemSchemaId);

        const string exceptionSchemaId = "ExceptionErrorResponse";
        _exceptionSchemaRef = context.SchemaRepository.AddDefinition(exceptionSchemaId, new()
        {
            Title = "Error Response",
            Description = "A response that represent a exception thrown by the server.",
            Example = OpenApiAnyFactory.CreateFromJson(
                """
                {
                  "error": {
                    "code": "Exception",
                    "message": "This is a example error response.",
                    "innerError": null
                  },
                  "traceId": ""
                }
                """)
        });
        context.SchemaRepository.RegisterType(typeof(Exception), exceptionSchemaId);
    }
}
