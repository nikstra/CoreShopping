using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
//using nikstra.CoreShopping.Web.Data;
using nikstra.CoreShopping.Web.Models;
using nikstra.CoreShopping.Web.Services;
using nikstra.CoreShopping.Service.Data;
using nikstra.CoreShopping.Service.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace nikstra.CoreShopping.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<UserDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentityCore<ShopUser>()
                .AddUserStore<UserRepository>()
                .AddDefaultTokenProviders();

            // BEGIN: Code "stolen" from Microsoft.AspNetCore.Identity.AddIdentity() since
            // the constraints is preventing the ShopRole from beeing used as type parameter for TRole.
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddCookie(IdentityConstants.ApplicationScheme, o =>
            {
                o.LoginPath = new PathString("/Account/Login");
                o.Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync
                };
            })
            .AddCookie(IdentityConstants.ExternalScheme, o =>
            {
                o.Cookie.Name = IdentityConstants.ExternalScheme;
                o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddCookie(IdentityConstants.TwoFactorRememberMeScheme,
                o => o.Cookie.Name = IdentityConstants.TwoFactorRememberMeScheme)
            .AddCookie(IdentityConstants.TwoFactorUserIdScheme, o =>
            {
                o.Cookie.Name = IdentityConstants.TwoFactorUserIdScheme;
                o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            });

            services.AddHttpContextAccessor();

            // Identity services
            services.TryAddScoped<IUserValidator<ShopUser>, UserValidator<ShopUser>>();
            services.TryAddScoped<IPasswordValidator<ShopUser>, PasswordValidator<ShopUser>>();
            services.TryAddScoped<IPasswordHasher<ShopUser>, PasswordHasher<ShopUser>>();
            services.TryAddScoped<ILookupNormalizer, UpperInvariantLookupNormalizer>();
            services.TryAddScoped<IRoleValidator<ShopRole>, RoleValidator<ShopRole>>();
            // No interface for the error describer so we can add errors without rev'ing the interface
            services.TryAddScoped<IdentityErrorDescriber>();
            services.TryAddScoped<ISecurityStampValidator, SecurityStampValidator<ShopUser>>();
            services.TryAddScoped<IUserClaimsPrincipalFactory<ShopUser>, UserClaimsPrincipalFactory<ShopUser, ShopRole>>();
            services.TryAddScoped<UserManager<ShopUser>, AspNetUserManager<ShopUser>>();
            services.TryAddScoped<SignInManager<ShopUser>, SignInManager<ShopUser>>();
            services.TryAddScoped<RoleManager<ShopRole>, AspNetRoleManager<ShopRole>>();
            // END: Code "stolen" from Microsoft.AspNetCore.Identity.AddIdentity().

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
