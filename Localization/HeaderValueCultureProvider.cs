using Microsoft.AspNetCore.Localization;
using WebSchoolPlanner.Extensions;

namespace WebSchoolPlanner.Localization;

/// <summary>
/// The <see cref="AcceptLanguageHeaderRequestCultureProvider"/> with redirect modification
/// </summary>
public class  HeaderValueCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (!this.IsLocalizablePath(httpContext))
            return NullProviderCultureResult;

        IRequestCultureProvider cultureProvider = new AcceptLanguageHeaderRequestCultureProvider
        {
            MaximumAcceptLanguageHeaderValuesToTry = 10,
            Options = Options
        };

        ProviderCultureResult? result = cultureProvider.DetermineProviderCultureResult(httpContext).Result;
        if (result is null)
            return NullProviderCultureResult;

        // Default results
        string culture = Options!.DefaultRequestCulture.Culture.Name;
        string uiCulture = Options!.DefaultRequestCulture.UICulture.Name;

        // Iterate the cultures and select the best
        foreach (string resCulture in result.Cultures
            .Reverse()
            .Select(ss => ss.Value!))
        {
            if (Options!.SupportedCultures!.Any(c => c.Name == resCulture))     // Exactly match
                culture = resCulture;
            else if (Options!.SupportedCultures!.Any(c => c.Name == resCulture[..2]))
                culture = resCulture[..2];
        }

        // Iterate the ui cultures and select the best
        foreach (string resCulture in result.UICultures
            .Reverse()
            .Select(ss => ss.Value!))
        {
            if (Options!.SupportedUICultures!.Any(c => c.Name == resCulture))     // Exactly match
                uiCulture = resCulture;
            else if (Options!.SupportedUICultures!.Any(c => c.Name == resCulture[..2]))
                uiCulture = resCulture[..2];
        }

        // Redirect if necessary
        string newPath = '/' + uiCulture + httpContext.Request.Path.Value!;
        httpContext.Response.Redirect(newPath);

        return Task.FromResult<ProviderCultureResult?>(new(culture, uiCulture));
    }
}
