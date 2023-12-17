﻿using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace WebSchoolPlanner.Extensions;

public static class ControllerBaseExtensions
{
    /// <summary>
    /// Redirects the request to the <paramref name="returnUrl"/> after the url is validated that it is local
    /// </summary>
    /// <remarks>
    /// If the <paramref name="returnUrl"/> isn't local the request would be redirected to the dashboard
    /// </remarks>
    /// <param name="controller">The controller</param>
    /// <param name="returnUrl">The url to redirect</param>
    /// <returns>The redirect result</returns>
    public static IActionResult RedirectToReturnUrl(this ControllerBase controller, string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
            return controller.RedirectToAction("Index", "Dashboard");

        bool isValid = Uri.TryCreate(returnUrl, UriKind.RelativeOrAbsolute, out Uri? uri);
        isValid &= !uri?.IsAbsoluteUri ?? false;

        if (!isValid)
            return controller.RedirectToAction("Index", "Dashboard");     // Redirect to dashboard
        return controller.Redirect(returnUrl);
    }

    /// <summary>
    /// Returns the api unauthorized response
    /// </summary>
    public static ObjectResult ApiUnauthorized(this ControllerBase controller)
    {
        return controller.Problem(
            type: "Unauthorized",
            detail: "The requested method require an authentication.",
            statusCode: StatusCodes.Status401Unauthorized);
    }

    /// <summary>
    /// Returns the api forbidden response
    /// </summary>
    public static ObjectResult ApiForbidden(this ControllerBase controller) =>
        ApiForbidden(controller, "The signed in user doesn't have the required permissions.");

    /// <summary>
    /// Returns the api forbidden response
    /// </summary>
    /// <param name="controller">The controller</param>
    /// <param name="reason">The reason</param>
    /// <returns></returns>
    public static ObjectResult ApiForbidden(this ControllerBase controller, string reason)
    {
        return controller.Problem(
            type: "Forbidden",
            detail: reason,
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
