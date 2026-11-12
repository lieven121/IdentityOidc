using Humanizer;
using Identity.App.Data;
using Identity.App.EndPoints.Identity.Models;
using Identity.App.Hosting;
using Identity.App.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;

namespace Identity.App.EndPoints.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("");

        group
            .WithTags("Identity")
            .WithName("Identity");

        group.MapPost("/login", LoginHandler)
            .WithName("Login")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingConfig.LoginPolicy);

        group.MapPost("/passkey/request-options", PasskeyRequestOptionsHandler)
            .WithName("PasskeyRequestOptions")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingConfig.LoginPolicy);

        group.MapPost("/passkey/login", PasskeyLoginHandler)
            .WithName("PasskeyLogin")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingConfig.LoginPolicy);

        group.MapPost("/logout", LogoutHandler)
            .WithName("Logout")
            .RequireAuthorization();


        #region Handlers


        async Task<IResult> LoginHandler(
                       LoginRequest dto,

                       SignInManager<ApplicationUser> signInManager,
                       UserManager<ApplicationUser> userManager,
                       SecurityAudit audit,
                       HttpContext httpContext)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);

            var isPersistent = true;

            if (string.IsNullOrWhiteSpace(user?.UserName))
            {
                audit.Log(SecurityEvent.LoginFailed, success: false, detail: $"unknown email '{dto.Email}'");
                await Task.Delay(Random.Shared.Next(100, 500));
                return Results.Unauthorized();
            }

            var result = await signInManager.PasswordSignInAsync(user.UserName, dto.Password, isPersistent, lockoutOnFailure: true);

            if (result.RequiresTwoFactor)
            {
                var codeSupplied = true;
                if (!string.IsNullOrEmpty(dto.TwoFactorCode))
                {
                    result = await signInManager.TwoFactorAuthenticatorSignInAsync(dto.TwoFactorCode, isPersistent, rememberClient: isPersistent);
                }
                else if (!string.IsNullOrEmpty(dto.TwoFactorRecoveryCode))
                {
                    result = await signInManager.TwoFactorRecoveryCodeSignInAsync(dto.TwoFactorRecoveryCode);
                }
                else
                {
                    codeSupplied = false;
                }

                if (result.IsLockedOut)
                {
                    audit.Log(SecurityEvent.LoginLockedOut, success: false, userId: user.Id);
                    return Results.Unauthorized();
                }
                if (!result.Succeeded)
                {
                    if (codeSupplied)
                        audit.Log(SecurityEvent.LoginTwoFactorFailed, success: false, userId: user.Id);
                    return Results.Accepted("Otp Required");
                }
            }

            //var user = await userManager.FindByEmailAsync("admin@localhost");

            //await signInManager.SignInAsync(user, true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    audit.Log(SecurityEvent.LoginLockedOut, success: false, userId: user.Id);
                else
                    audit.Log(SecurityEvent.LoginFailed, success: false, userId: user.Id,
                        detail: result.IsNotAllowed ? "sign-in not allowed (unconfirmed account)" : "invalid password");
                return Results.Unauthorized();
            }

            audit.Log(SecurityEvent.LoginSucceeded, userId: user.Id);
            return Results.Ok("Logged In");
        }

        // Usernameless: no user is passed, so the browser offers any discoverable passkey for this site.
        async Task<Ok<PasskeyOptionsDto>> PasskeyRequestOptionsHandler(SignInManager<ApplicationUser> signInManager)
        {
            var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(null);
            return TypedResults.Ok(new PasskeyOptionsDto { OptionsJson = optionsJson });
        }

        // A passkey is device possession plus biometric/PIN, so it counts as MFA and skips the OTP step.
        // Mirrors SignInManager.PasskeySignInAsync, but signs in persistently like the password login.
        async Task<Results<Ok<string>, UnauthorizedHttpResult>> PasskeyLoginHandler(
            PasskeyCredentialDto dto,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            SecurityAudit audit)
        {
            PasskeyAssertionResult<ApplicationUser> assertion;
            try
            {
                assertion = await signInManager.PerformPasskeyAssertionAsync(dto.CredentialJson);
            }
            catch (InvalidOperationException)
            {
                // No (or expired) assertion state cookie.
                audit.Log(SecurityEvent.LoginFailed, success: false, detail: "passkey: no sign-in in progress");
                return TypedResults.Unauthorized();
            }
            if (!assertion.Succeeded)
            {
                audit.Log(SecurityEvent.LoginFailed, success: false, detail: $"passkey: {assertion.Failure.Message}");
                return TypedResults.Unauthorized();
            }

            var user = assertion.User;
            if (await userManager.IsLockedOutAsync(user))
            {
                audit.Log(SecurityEvent.LoginLockedOut, success: false, userId: user.Id, detail: "passkey");
                return TypedResults.Unauthorized();
            }
            if (!await signInManager.CanSignInAsync(user))
            {
                audit.Log(SecurityEvent.LoginFailed, success: false, userId: user.Id, detail: "passkey: sign-in not allowed");
                return TypedResults.Unauthorized();
            }

            // Stores the new signature counter and backup state.
            var updateResult = await userManager.AddOrUpdatePasskeyAsync(user, assertion.Passkey);
            if (!updateResult.Succeeded)
            {
                audit.Log(SecurityEvent.LoginFailed, success: false, userId: user.Id,
                    detail: $"passkey: {string.Join(", ", updateResult.Errors.Select(e => e.Code))}");
                return TypedResults.Unauthorized();
            }

            await signInManager.SignInAsync(user, isPersistent: true, authenticationMethod: "passkey");
            audit.Log(SecurityEvent.LoginSucceeded, userId: user.Id, detail: "passkey");
            return TypedResults.Ok("Logged In");
        }

        async Task LogoutHandler(SignInManager<ApplicationUser> signInManager, SecurityAudit audit)
        {
            audit.Log(SecurityEvent.Logout);
            await signInManager.SignOutAsync();
        }

        #endregion

        return app;
    }


}
