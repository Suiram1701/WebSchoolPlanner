using Microsoft.AspNetCore.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using WebSchoolPlanner.Middlewares;

namespace WebSchoolPlanner.Extensions;

public static class IApplicationBuilderExtensions
{
    /// <summary>
    /// Use the default database request only for requests that renders a view
    /// </summary>
    /// <returns>The request pipeline</returns>
    public static IApplicationBuilder UseDatabaseRequestMiddleware(this IApplicationBuilder app)
    {
        return app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), subApp => subApp.UseMiddleware<DatabaseRequestMiddleware>());
    }

    /// <summary>
    /// Use the RequestLocalizationMiddleware only for not api requests
    /// </summary>
    /// <returns>The request pipeline</returns>
    public static IApplicationBuilder UseLocalization(this IApplicationBuilder app)
    {
        return app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), subApp => subApp.UseRequestLocalization());
    }

    /// <summary>
    /// Use the <see cref="ApiAuthorizeMiddleware"/> if it is a api request
    /// </summary>
    /// <returns>The request pipeline</returns>
    public static IApplicationBuilder UseApiAuthorization(this IApplicationBuilder app)
    {
        return app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), subApp => subApp.UseMiddleware<ApiAuthorizeMiddleware>());
    }

    /// <summary>
    /// Return Api exceptions as json
    /// </summary>
    /// <returns>The request pipeline</returns>
    public static IApplicationBuilder UseApiExceptionHandler(this IApplicationBuilder app)
    {
        app.UseWhen(
            predicate: context => context.Request.Path.StartsWithSegments("/api"),
            configuration: subApp => subApp.UseExceptionHandler(builder => builder.Run(HandleApiExceptionAsync)));

        return app;
    }

    private static async Task HandleApiExceptionAsync(HttpContext context)
    {
        IExceptionHandlerPathFeature? exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        Exception? exception = exceptionFeature?.Error;

        // Determine the response code
        int responseCode;
        switch (exception?.GetType().Name)
        {
            case nameof(ArgumentException):
            case nameof(ArgumentNullException):
            case nameof(ArgumentOutOfRangeException):
            case nameof(UnknownImageFormatException):
            case nameof(JsonException):
            case nameof(JsonReaderException):
            case nameof(FormatException):
                responseCode = StatusCodes.Status400BadRequest;
                break;
            case nameof(NotImplementedException):
                responseCode = StatusCodes.Status501NotImplemented;
                break;
            default:
                responseCode = StatusCodes.Status500InternalServerError;
                break;
        }

        // Write the exception as json
        StringWriter sw = new(new StringBuilder());
        using JsonWriter writer = new JsonTextWriter(sw);

        void WriteException(Exception? ex)
        {
            if (ex is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            string exceptionName = ex.GetType().Name;
            int suffixIndex = exceptionName.IndexOf(nameof(Exception));
            string code = exceptionName.Remove(suffixIndex);

            writer.WritePropertyName("code");
            writer.WriteValue(code);

            writer.WritePropertyName("message");
            writer.WriteValue(ex.Message);

            writer.WritePropertyName("innerError");
            WriteException(ex.InnerException);

            writer.WriteEndObject();
        }

        writer.WriteStartObject();

        writer.WritePropertyName("error");
        WriteException(exception);

        writer.WritePropertyName("traceId");
        writer.WriteValue(context.TraceIdentifier);

        writer.WriteEndObject();

        string response = sw.ToString();

        context.Response.ContentType = "application/json";
        context.Response.ContentLength = response.Length;
        context.Response.StatusCode = responseCode;
        await context.Response.WriteAsync(response);
    }
}
