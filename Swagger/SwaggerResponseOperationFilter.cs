using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace WebSchoolPlanner.Swagger;

/// <summary>
/// The default operation filter for swagger that adds error responses
/// </summary>
public class SwaggerResponseOperationFilter : IOperationFilter
{
    private const string _jsonResponse = "application/json";
    private const string _jsonProblemReponse = "application/problem+json";

#pragma warning disable CS8618
    private OpenApiSchema _problemSchemaRef;
    private OpenApiSchema _exceptionSchemaRef;
#pragma warning restore CS8618

    void IOperationFilter.Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!context.SchemaRepository.Schemas.Any())
            SetupSchemas(context);

        // Add default responses
        if (operation.Parameters.Any())
        {
            operation.Responses.Add(StatusCodes.Status400BadRequest.ToString(), new()
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    {
                        _jsonResponse,
                        new() { Schema = _exceptionSchemaRef }
                    },
                    {
                        _jsonProblemReponse,
                        new() { Schema = _problemSchemaRef }
                    }
                }
            });
        }

        if (context.MethodInfo.GetCustomAttribute<AllowAnonymousAttribute>() is null)
        {
            operation.Responses.Add(StatusCodes.Status401Unauthorized.ToString(), new()
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    {
                        _jsonProblemReponse,
                        new() { Schema = _problemSchemaRef }
                    }
                }
            });
        }

        if (operation.Parameters.Any())
        {
            if (operation.Parameters.Any(p => p.In == ParameterLocation.Header &&
                (p.Name == HeaderNames.Accept
                || p.Name == HeaderNames.AcceptEncoding
                || p.Name == HeaderNames.AcceptLanguage)))
            {
                operation.Responses.Add(StatusCodes.Status406NotAcceptable.ToString(), new()
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            _jsonProblemReponse,
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
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    {
                        _jsonProblemReponse,
                        new() { Schema = _problemSchemaRef }
                    }
                }
            });
        }

        operation.Responses.Add(StatusCodes.Status500InternalServerError.ToString(), new()
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                {
                    _jsonResponse,
                    new() { Schema = _exceptionSchemaRef }
                }
            }
        });
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
                    "traceId": ""
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
