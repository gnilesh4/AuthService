using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService
{
    public class Startup
    {
        public void Configure
        (
            IApplicationBuilder application,
            ApplicationDbContext applicationDbContext,
            ConfigurationDbContext configurationDbContext,
            PersistedGrantDbContext persistedGrantDbContext,
            UserManager<IdentityUser> userManager
        )
        {
            applicationDbContext.Database.Migrate();
            configurationDbContext.Database.Migrate();
            persistedGrantDbContext.Database.Migrate();

            configurationDbContext.Seed();
            userManager.Seed();

            application.UseException();
            application.UseCookiePolicy();
            application.UseCorsAllowAny();
            application.UseHsts();
            application.UseHttpsRedirection();
            application.UseStaticFiles();
            application.UseRouting();
            application.UseIdentityServer();
            application.UseAuthentication();
            application.UseAuthorization();
            application.UseEndpoints();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAppSettings();
            services.AddCors();
            services.AddControllersWithViews();
            services.AddAuthentication().AddCookie();
            services.AddCookie();
            services.AddIdentity();
            services.AddIdentityServerConfiguration();
            services.AddApplicationDbContext();
        }
    }
}
