using IdentityServer4;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace AuthService
{
    public static class Extensions
    {
        public static IServiceCollection AddApplicationDbContext(this IServiceCollection services)
        {
            var appSettings = services.BuildServiceProvider().GetRequiredService<AppSettings>();

            return services.AddDbContext<ApplicationDbContext>(builder => builder.Connection(appSettings.ConnectionStrings.Database));
        }

        public static AppSettings AddAppSettings(this IServiceCollection services)
        {
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            var appSettings = configuration.Get<AppSettings>();

            services.AddSingleton(appSettings);

            return appSettings;
        }

        public static AuthenticationBuilder AddCookie(this AuthenticationBuilder builder)
        {
            return builder.AddCookie(options =>
             {
                 options.Cookie.HttpOnly = true;
                 options.Cookie.SameSite = SameSiteMode.Lax;
                 options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
             });
        }

        public static IServiceCollection AddCookie(this IServiceCollection services)
        {
            services.AddAntiforgery(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.HttpOnly = HttpOnlyPolicy.Always;
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
                options.Secure = CookieSecurePolicy.Always;
            });

            return services;
        }

        public static IServiceCollection AddCors(this IServiceCollection services)
        {
            return services.AddCors(options => options.AddPolicy("AllowAny", policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
        }

        public static IdentityBuilder AddIdentity(this IServiceCollection services)
        {
            return services
                .AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
        }

        public static IServiceCollection AddIdentityServerConfiguration(this IServiceCollection services)
        {
            services
                .AddIdentityServer()
                .AddConfigurationStore()
                .AddOperationalStore()
                .AddAspNetIdentity<IdentityUser>()
                .AddSigningCredential();

            return services
                .AddLocalApiAuthentication()
                .AddOidcStateDataFormatterCache("aad")
                .AddTransient<IRedirectUriValidator, RedirectUriValidator>()
                .AddTransient<ICorsPolicyService, CorsPolicyService>();
        }

        public static void Connection(this DbContextOptionsBuilder builder, string connectionString)
        {
            builder.UseSqlite(connectionString, options => options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName));
        }

        public static void Connection(this DbContextOptionsBuilder builder)
        {
            builder.Connection("Data Source=Auth.db;");
        }

        public static IApplicationBuilder UseCorsAllowAny(this IApplicationBuilder application)
        {
            return application.UseCors("AllowAny");
        }

        public static IApplicationBuilder UseEndpoints(this IApplicationBuilder application)
        {
            return application.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());
        }

        public static IApplicationBuilder UseException(this IApplicationBuilder application)
        {
            var environment = application.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

            if (environment.IsDevelopment())
            {
                application.UseDeveloperExceptionPage();
            }

            return application;
        }

        private static IIdentityServerBuilder AddConfigurationStore(this IIdentityServerBuilder builder)
        {
            var appSettings = builder.Services.BuildServiceProvider().GetRequiredService<AppSettings>();

            return builder.AddConfigurationStore(options => options.ConfigureDbContext = optionsBuilder => optionsBuilder.Connection(appSettings.ConnectionStrings.Database));
        }

        private static AuthenticationBuilder AddExternalAzure(this AuthenticationBuilder builder)
        {
            var appSettings = builder.Services.BuildServiceProvider().GetRequiredService<AppSettings>();

            return builder.AddOpenIdConnect("aad", "Azure", options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.SignOutScheme = IdentityServerConstants.SignoutScheme;
                options.CallbackPath = "/signin-aad";
                options.RemoteSignOutPath = "/signout-aad";
                options.ResponseType = "id_token";
                options.SignedOutCallbackPath = "/signout-callback-aad";
                options.Authority = appSettings.Azure.Authority;
                options.ClientId = appSettings.Azure.ClientId;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = !string.IsNullOrWhiteSpace(appSettings.Azure.Audience),
                    ValidateIssuer = !string.IsNullOrWhiteSpace(appSettings.Azure.Issuer),
                    ValidAudience = appSettings.Azure.Audience,
                    ValidIssuer = appSettings.Azure.Issuer,
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });
        }

        private static AuthenticationBuilder AddExternalGoogle(this AuthenticationBuilder builder)
        {
            var appSettings = builder.Services.BuildServiceProvider().GetRequiredService<AppSettings>();

            return builder.AddGoogle("Google", options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.ClientId = appSettings.Google.ClientId;
                options.ClientSecret = appSettings.Google.ClientSecret;
            });
        }

        private static IIdentityServerBuilder AddOperationalStore(this IIdentityServerBuilder builder)
        {
            var appSettings = builder.Services.BuildServiceProvider().GetRequiredService<AppSettings>();

            return builder.AddOperationalStore(options => options.ConfigureDbContext = optionsBuilder => optionsBuilder.Connection(appSettings.ConnectionStrings.Database));
        }

        private static IIdentityServerBuilder AddSigningCredential(this IIdentityServerBuilder builder)
        {
            var environment = builder.Services.BuildServiceProvider().GetRequiredService<IWebHostEnvironment>();

            if (environment.IsDevelopment())
            {
                var file = Path.Combine(Environment.CurrentDirectory, "Certificate.pfx");

                var certificate = new X509Certificate2(file, "123456", X509KeyStorageFlags.Exportable);

                builder.AddSigningCredential(certificate);
            }

            return builder;
        }
    }
}
