using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using WebSchoolPlanner.Extensions;

namespace WebSchoolPlanner.Localization;

/// <summary>
/// The <see cref="RouteDataRequestCultureProvider"/> with redirect modification
/// </summary>
public class RouteValueCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (!this.IsLocalizablePath(httpContext))
            return NullProviderCultureResult;

        IRequestCultureProvider cultureProvider = new RouteDataRequestCultureProvider
        {
            RouteDataStringKey = "languageCode",
            UIRouteDataStringKey = "languageCode",
            Options = Options
        };

        ProviderCultureResult? result = cultureProvider.DetermineProviderCultureResult(httpContext).Result;
        if (result is null)
            return NullProviderCultureResult;

        if (!RegExes.CultureRegex().IsMatch(result.UICultures[0]))
            return NullProviderCultureResult;

        // Default results
        string culture = Options!.DefaultRequestCulture.Culture.Name;
        string uiCulture = Options!.DefaultRequestCulture.UICulture.Name;

        // Culture
        if (Options!.SupportedCultures!.Any(c => c.Name == result.Cultures[0]))     // Exactly match
            culture = result.Cultures[0].Value!;
        else if (Options!.SupportedCultures!.Any(c => c.Name == result.Cultures[0].Value![..2]))
            culture = result.Cultures[0].Value![..2];

        // UI culture
        if (Options!.SupportedUICultures!.Any(c => c.Name == result.UICultures[0]))     // Exactly match
            uiCulture = result.UICultures[0].Value!;
        else if (Options!.SupportedUICultures!.Any(c => c.Name == result.UICultures[0].Value![..2]))
            uiCulture = result.UICultures[0].Value![..2];

        // Redirect if necessary
        if (uiCulture != result.UICultures[0].Value)
        {
            string newPath = httpContext.Request.Path.Value!.Replace(result.UICultures[0].Value!, uiCulture);
            httpContext.Response.Redirect(newPath);
        }

        return Task.FromResult<ProviderCultureResult?>(new(culture, uiCulture));
    }
}
