using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace WebSchoolPlanner.Extensions;

public static class ControllerBaseExtensions
{
    /// <summary>
    /// Returns the api unauthorized response
    /// </summary>
    public static ObjectResult ApiUnauthorized(ControllerBase controller)
    {
        return controller.Problem(
            type: "Unauthorized",
            detail: "The requested method require an authentication.",
            statusCode: StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Returns the api forbidden response
    /// </summary>
    public static ObjectResult ApiForbidden(this ControllerBase controller)
    {
        return controller.Problem(
            type: "Forbidden",
            detail: string.Format("The requested method requires the needed authorization."),
            statusCode: StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Returns the file with their SHA256 integrity hash.
    /// </summary>
    /// <param name="controller">The controller</param>
    /// <param name="stream">The file stream</param>
    /// <param name="contentType">The MIME content type</param>
    /// <returns>The file result</returns>
    public static FileStreamResult FileWithIntegrityHash(this ControllerBase controller, Stream stream, string contentType)
    {
        // Calc the hash
        using SHA256 sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(stream);

        string headerValue = BitConverter.ToString(hash).Replace("-", "").ToLower();
        controller.HttpContext.Response.Headers["Content-SHA256"] = headerValue;

        stream.Seek(0, SeekOrigin.Begin);
        return controller.File(stream, contentType);
    }
}
