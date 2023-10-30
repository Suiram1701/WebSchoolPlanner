using Humanizer;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Globalization;
using WebSchoolPlanner.Extensions;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Reflection;
using WebSchoolPlanner.Db.Models;
using Microsoft.AspNetCore.Identity;
using WebSchoolPlanner.Db;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace WebSchoolPlanner;

public class Startup
{
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
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix, options => options.ResourcesPath = "Localization");

        // Localization
        services
            .AddLocalization(options => options.ResourcesPath = "Localization")
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
                options.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    new CookieRequestCultureProvider
                    {
                        Options = options
                    },
                    new AcceptLanguageHeaderRequestCultureProvider
                    {
                        Options = options,
                        MaximumAcceptLanguageHeaderValuesToTry = 10
                    }
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

        if (_configuration[SwaggerConfigurationPrefix + "Use"] == true.ToString())
            services.AddSwagger();

        // Database / authentication
        services
            .AddDbContext<WebSchoolPlannerDbContext>(options =>
            {
                string connectionString = _configuration.GetConnectionString("WebSchoolPlannerDbContext")
                    ?? throw new ArgumentNullException(nameof(connectionString), "A connection string for the database 'WebSchoolPlannerDbContext' is required.");
                options.UseSqlServer(connectionString);

            }, ServiceLifetime.Singleton, ServiceLifetime.Transient)
            .AddIdentity<User, Role>(options =>
            {
                int maxFailedLoginAttempts = int.Parse(_configuration["Account:MaxFailedSignInAttempts"] ?? "5");
                double lockOutSeconds = double.Parse(_configuration["Account:LockOutTimeSpan"] ?? "300");

                options.Lockout.MaxFailedAccessAttempts = maxFailedLoginAttempts;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromSeconds(lockOutSeconds);
            })
            .AddEntityFrameworkStores<WebSchoolPlannerDbContext>()
            .AddDefaultTokenProviders();

        // Security
        services
            .AddAntiforgery(options =>
            {
                options.Cookie.Name = ".AspNetCore.CSRF.TOKEN";
                options.FormFieldName = "_CSRF-TOKEN";
                options.HeaderName = "X-CSRF-TOKEN";
            })
            .ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";

                options.SlidingExpiration = true;
                options.ReturnUrlParameter = "r";
            });

        services
            .AddCookiePolicy(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.ConsentCookie.Name = ".AspNetCore.acceptCookiePolicy";
                options.ConsentCookieValue = "true";
                options.MinimumSameSitePolicy = SameSiteMode.None;
            })
            .AddSession();

        services.AddAuthentication();
        services.AddAuthorization();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Error handling
        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();
        else
            app.UseHsts();

        app.UseApiExceptionHandler();

        // Default request pipeline
        app
            .UseHttpsRedirection()
            .UseStaticFiles()
            .UseCookiePolicy();

        // Api / Swagger
        app.UseApiVersioning();
        if (_configuration[SwaggerConfigurationPrefix + "Use"] == true.ToString())
            app.UseSwagger();

        if (_configuration[SwaggerConfigurationPrefix + "UseUI"] == true.ToString())
        {
            app.UseSwaggerUI(options =>
            {
                options.SupportedSubmitMethods(SubmitMethod.Get);     // Enable the 'Try it out' feature only for GET requests

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

        // Routing
        app
            .UseRouting()
            .UseSession()
            .UseAuthentication()
            .UseAuthorization()
            .UseRequestLocalization()
            .UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
