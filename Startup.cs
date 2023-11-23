﻿using Humanizer;
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
using WebSchoolPlanner.Localization;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebSchoolPlanner.Db.Stores;

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
            .AddMvc()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix, options => options.ResourcesPath = "Localization");

        // Localization
        services
            .AddLocalization(options => options.ResourcesPath = "Localization")
            .AddRequestLocalization(options =>
            {
                List<CultureInfo> cultures = new()
                {
                    new("en"),
                    new("es"),
                    new("de"),
                    new("fr"),
                    new("ru")
                };

                // Set cultures
                options.SetDefaultCulture(cultures[0].Name);
                options.AddSupportedUICultures(cultures.Select(c => c.Name).ToArray());
                options.AddSupportedCultures(cultures.Select(c => c.Name).ToArray());

                // Other options
                options.FallBackToParentUICultures = true;
                options.FallBackToParentCultures = true;
                options.ApplyCurrentCultureToResponseHeaders = true;

                // Providers
                options.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    new AccountRequestCultureProvider
                    {
                        Options = options
                    },
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
        services.AddSingleton<UserImageStore<User>, UserImageStore<User>>();

        // Security
        services
            .AddAuthorization()
            .AddAntiforgery(options =>
            {
                options.Cookie.Name = ".AspNetCore.CSRF.TOKEN";
                options.FormFieldName = "_CSRF-TOKEN";
                options.HeaderName = "X-CSRF-TOKEN";
            })
            .ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/auth/login";
                options.LogoutPath = "/auth/logout";

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
                options.SupportedSubmitMethods();     // Disable 'Try it out' feature

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
            .UseApiAuthorization()
            .UseAuthorization()
            .UseLocalization()
            .UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
