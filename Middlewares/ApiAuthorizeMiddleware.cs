using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Reflection;

namespace WebSchoolPlanner.Middlewares;

public class ApiAuthorizeMiddleware
{
    private readonly RequestDelegate _next;

    public ApiAuthorizeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        Endpoint endpoint = context.GetEndpoint()!;
        if (!endpoint.Metadata.OfType<AllowAnonymousAttribute>().Any())
        {
            if (!(context.User.Identity?.IsAuthenticated ?? false))
            {
                // Write response
                using StringWriter sw = new();
                using JsonWriter writer = new JsonTextWriter(sw);

                writer.WriteStartObject();

                writer.WritePropertyName("type");
                writer.WriteValue("Unauthorized");

                writer.WritePropertyName("detail");
                writer.WriteValue("The requested method require an authentication.");

                writer.WritePropertyName("status");
                writer.WriteValue(StatusCodes.Status401Unauthorized);

                writer.WritePropertyName("traceId");
                writer.WriteValue(context.TraceIdentifier);

                writer.WriteEndObject();

                context.Response.ContentType = "application/json+problem";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync(sw.ToString());

                return;
            }
        }

        await _next(context);
    }
}
