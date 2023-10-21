using Humanizer;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Globalization;
using WebSchoolPlanner.Extensions;
using Microsoft.AspNetCore.Mvc.Razor;
using WebSchoolPlanner.RouteConstraints;
using System.Reflection;
using WebSchoolPlanner.Db.Models;
using Microsoft.AspNetCore.Identity;
using WebSchoolPlanner.Db;
using Microsoft.EntityFrameworkCore;

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
        // Routing
        services.Configure<RouteOptions>(options =>
        {
            IEnumerable<RouteConstraintAttribute> attributes = typeof(Program).Assembly.GetTypes()
                .Where(type => type.GetInterface(nameof(IRouteConstraint)) is not null)     // Only types that implement IRouteConstraint
                .Select(type => type.GetCustomAttribute<RouteConstraintAttribute>())
                .Where(attribute => attribute is not null)!;     // Filter types that don't have the attribute

            // Add custom route constraints
            foreach (RouteConstraintAttribute attribute in attributes)
                options.ConstraintMap.Add(attribute.Name, attribute.Type);
        });

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
                    new RouteDataRequestCultureProvider
                    {
                        Options = options,
                        RouteDataStringKey = LanguageRouteKey,
                        UIRouteDataStringKey = LanguageRouteKey
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

        if (_configuration["Swagger:Use"] == true.ToString())
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
                options.Cookie.Name = "CSRF_TOKEN";
                options.FormFieldName = "_CSRF-TOKEN";
                options.HeaderName = "X-CSRF-TOKEN";
            })
            .ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";

                options.SlidingExpiration = true;
                options.ReturnUrlParameter = "returnUrl";
            });

        services
            .AddCookiePolicy(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.ConsentCookie.Name = "acceptCookiePolicy";
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

        // Default request pipeline
        app
            .UseHttpsRedirection()
            .UseStaticFiles()
            .UseCookiePolicy();

        // Routing
        app
            .UseRouting()
            .UseLocalization("/api", "/swagger")
            .UseSession()
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(endpoints => endpoints.MapControllers());

        // Api / Swagger
        app.UseApiVersioning();
        if (_configuration["Swagger:Use"] == true.ToString())
            app.UseSwagger();

        if (_configuration["Swagger:UseUI"] == true.ToString())
        {
            app.UseSwaggerUI(options =>
            {
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
    }
}
