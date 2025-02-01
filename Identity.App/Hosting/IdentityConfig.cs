using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using Identity.App.Data;

namespace Identity.App.Hosting;
public static class IdentityConfig
{
    public static IHostApplicationBuilder ConfigureIdentity(this IHostApplicationBuilder app)
    {

        app.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.SignIn.RequireConfirmedPhoneNumber = false;

                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;

                options.User.RequireUniqueEmail = true;

            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        app.Services.Configure<IdentityOptions>(options =>
        {
            options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Email;
            //options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
            options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
            options.ClaimsIdentity.RoleClaimType = OpenIddictConstants.Claims.Role;
        });

        app.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        })
        .AddIdentityCookies(options =>
        {
            options?.ApplicationCookie?.Configure(c =>
            {
                c.LoginPath = "/Login";
            });
        });

        app.Services.AddAuthorization();


        return app;
    }

    public static IApplicationBuilder UseIdentity(this WebApplication app)
    {
        app.UseCors();

        return app;
    }
}
