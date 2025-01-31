using Humanizer;
using Identity.App.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;

namespace Identity.App.EndPoints.Identity;

public static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("");

        group.MapGet("/api/test", (HttpContext httpContext) =>
        {
            if (httpContext.User.Identity?.IsAuthenticated != true)
                return Results.Challenge();

            return Results.Ok(httpContext.User.Identity?.Name);
        });

        group.MapPost("/api/login", async (
            LoginRequest dto,

            SignInManager <ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            HttpContext httpContext) =>
        {

            var isPersistent = true;

            var result = await signInManager.PasswordSignInAsync(dto.Email, dto.Password, isPersistent, false);

            if(result.RequiresTwoFactor)
            {
                if (!string.IsNullOrEmpty(dto.TwoFactorCode))
                {
                    result = await signInManager.TwoFactorAuthenticatorSignInAsync(dto.TwoFactorCode, isPersistent, rememberClient: isPersistent);
                }
                else if (!string.IsNullOrEmpty(dto.TwoFactorRecoveryCode))
                {
                    result = await signInManager.TwoFactorRecoveryCodeSignInAsync(dto.TwoFactorRecoveryCode);
                }
                if(!result.Succeeded)
                    return Results.Accepted("Otp Required");
            }

            //var user = await userManager.FindByEmailAsync("admin@localhost");

            //await signInManager.SignInAsync(user, true);

            return Results.Ok("Logged In");
        });

        group.MapPost("/api/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
        })
        .RequireAuthorization();


    }
}
