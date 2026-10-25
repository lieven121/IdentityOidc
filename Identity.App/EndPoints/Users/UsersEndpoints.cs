using Identity.App.Data;
using Identity.App.EndPoints.Identity.Models;
using Identity.App.EndPoints.Users.Models;
using Identity.App.Hosting;
using Identity.App.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Identity.App.EndPoints.Users;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("users")
            .WithTags("Users")
            .WithName("Users");


        group.MapGet("me", CurrentUserHandler)
            .WithName("CurrentUser")
            .AllowAnonymous();

        group.MapPost("me", UpdateUserHandler)
            .WithName("UpdateUser");

        group.MapPost("me/password", UpdatePasswordHandler)
            .WithName("UpdatePassword");

        group.MapGet("me/roles", GetCurrentUserRolesHandler)
            .WithName("GetCurrentUserRoles");

        group.MapGet("me/2fa/status", Get2faStatusHandler)
            .WithName("Get2faStatus");

        group.MapGet("me/2fa/setup", Get2faSetupHandler)
            .WithName("Get2faSetup");

        group.MapPost("me/2fa/enable", Enable2faHandler)
            .WithName("Enable2fa")
            .RequireRateLimiting(RateLimitingConfig.LoginPolicy);

        group.MapPost("me/2fa/disable", Disable2faHandler)
            .WithName("Disable2fa")
            .RequireRateLimiting(RateLimitingConfig.LoginPolicy);

        group.MapPost("me/2fa/recovery-codes", GenerateRecoveryCodesHandler)
            .WithName("GenerateRecoveryCodes")
            .RequireRateLimiting(RateLimitingConfig.LoginPolicy);

        group.MapGet("me/passkeys", GetPasskeysHandler)
            .WithName("GetPasskeys");

        group.MapPost("me/passkeys/creation-options", PasskeyCreationOptionsHandler)
            .WithName("PasskeyCreationOptions")
            .RequireRateLimiting(RateLimitingConfig.LoginPolicy);

        group.MapPost("me/passkeys", AddPasskeyHandler)
            .WithName("AddPasskey")
            .RequireRateLimiting(RateLimitingConfig.LoginPolicy);

        group.MapDelete("me/passkeys/{id}", RemovePasskeyHandler)
            .WithName("RemovePasskey");

        return app;
    }

    private static async Task<Results<Ok<UserDto?>, ForbidHttpResult>> CurrentUserHandler(HttpContext httpContext, UserManager<ApplicationUser> userManager, TwoFactorPolicy twoFactorPolicy)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Ok<UserDto?>(null);

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        return TypedResults.Ok<UserDto?>(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.UserName,
            TwoFactorEnabled = user.TwoFactorEnabled,
            RequiresTwoFactorSetup = await twoFactorPolicy.NeedsSetupAsync(user),
        });
    }

    private static async Task<Results<Ok<UserDto>, ForbidHttpResult, UnauthorizedHttpResult>> UpdateUserHandler(
    [FromBody] UpdateUserDto updateUserDto,
    HttpContext httpContext,
    UserManager<ApplicationUser> userManager,
    TwoFactorPolicy twoFactorPolicy,
    SecurityAudit audit)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        var oldUserName = user.UserName;
        if (!string.IsNullOrWhiteSpace(updateUserDto.Username))
        {
            user.UserName = updateUserDto.Username;
        }

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            audit.Log(SecurityEvent.ProfileUpdated, success: false, userId: user.Id,
                detail: string.Join(", ", result.Errors.Select(e => e.Code)));
            return TypedResults.Unauthorized();
        }

        if (oldUserName != user.UserName)
            audit.Log(SecurityEvent.ProfileUpdated, userId: user.Id, detail: $"username '{oldUserName}' -> '{user.UserName}'");

        return TypedResults.Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            Username = user.UserName ?? "",
            TwoFactorEnabled = user.TwoFactorEnabled,
            RequiresTwoFactorSetup = await twoFactorPolicy.NeedsSetupAsync(user),
        });
    }


    private static async Task<Results<Ok, ForbidHttpResult, UnauthorizedHttpResult>> UpdatePasswordHandler(
        [FromBody] UpdatePasswordDto updatePasswordDto,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        SecurityAudit audit)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        var resetPasswordResult = await userManager.ChangePasswordAsync(user, updatePasswordDto.CurrentPassword, updatePasswordDto.NewPassword);
        if (!resetPasswordResult.Succeeded)
        {
            audit.Log(SecurityEvent.PasswordChanged, success: false, userId: user.Id,
                detail: string.Join(", ", resetPasswordResult.Errors.Select(e => e.Code)));
            return TypedResults.Unauthorized();
        }

        audit.Log(SecurityEvent.PasswordChanged, userId: user.Id);

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<IList<string>>, ForbidHttpResult, UnauthorizedHttpResult>> GetCurrentUserRolesHandler(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        var roles = await userManager.GetRolesAsync(user);
        return TypedResults.Ok(roles);
    }

    // Two-Factor Authentication Handlers

    private static async Task<Results<Ok<TwoFactorStatusDto>, ForbidHttpResult, UnauthorizedHttpResult>> Get2faStatusHandler(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        UserManagementService userManagementService)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        var status = await userManagementService.GetTwoFactorStatusAsync(user);
        if (status is null)
            return TypedResults.Forbid();

        return TypedResults.Ok(status);
    }

    private static async Task<Results<Ok<TwoFactorSetupDto>, ForbidHttpResult, UnauthorizedHttpResult, BadRequest>> Get2faSetupHandler(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        UserManagementService userManagementService,
        IConfiguration configuration)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        if (user.TwoFactorEnabled)
            return TypedResults.BadRequest();

        var appName = configuration["ApplicationConfig:Name"] ?? "Identity App";
        var setupInfo = await userManagementService.LoadSharedKeyAndQrCodeUriAsync(user, appName);
        if (setupInfo is null)
            return TypedResults.Forbid();

        return TypedResults.Ok(setupInfo);
    }

    private static async Task<Results<Ok<RecoveryCodesDto>, ForbidHttpResult, UnauthorizedHttpResult, BadRequest>> Enable2faHandler(
        [FromBody] Enable2faDto enable2faDto,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        UserManagementService userManagementService,
        SecurityAudit audit)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        if (user.TwoFactorEnabled)
            return TypedResults.BadRequest();

        var (success, recoveryCodes) = await userManagementService.EnableTwoFactorAsync(user, enable2faDto.Code);
        if (!success || recoveryCodes is null)
        {
            audit.Log(SecurityEvent.TwoFactorVerificationFailed, success: false, userId: user.Id, detail: "enable 2FA");
            return TypedResults.BadRequest();
        }

        audit.Log(SecurityEvent.TwoFactorEnabled, userId: user.Id);

        return TypedResults.Ok(new RecoveryCodesDto
        {
            RecoveryCodes = recoveryCodes
        });
    }

    private static async Task<Results<Ok, ForbidHttpResult, UnauthorizedHttpResult, BadRequest, BadRequest<string>>> Disable2faHandler(
        [FromBody] TwoFactorCodeDto twoFactorCodeDto,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        UserManagementService userManagementService,
        TwoFactorPolicy twoFactorPolicy,
        SecurityAudit audit)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        if (!user.TwoFactorEnabled)
            return TypedResults.BadRequest();

        if (await twoFactorPolicy.IsRequiredAsync(user) && (await userManager.GetPasskeysAsync(user)).Count == 0)
            return TypedResults.BadRequest("Two-factor authentication is required for your account. Add a passkey before disabling it.");

        // Require proof of the second factor, so a hijacked session can't strip 2FA.
        if (!await userManagementService.VerifyTwoFactorCodeAsync(user, twoFactorCodeDto.Code))
        {
            audit.Log(SecurityEvent.TwoFactorVerificationFailed, success: false, userId: user.Id, detail: "disable 2FA");
            return TypedResults.BadRequest();
        }

        var success = await userManagementService.DisableTwoFactorAsync(user);
        if (!success)
            return TypedResults.BadRequest();

        audit.Log(SecurityEvent.TwoFactorDisabled, userId: user.Id);

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<RecoveryCodesDto>, ForbidHttpResult, UnauthorizedHttpResult, BadRequest>> GenerateRecoveryCodesHandler(
        [FromBody] TwoFactorCodeDto twoFactorCodeDto,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        UserManagementService userManagementService,
        SecurityAudit audit)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        if (!user.TwoFactorEnabled)
            return TypedResults.BadRequest();

        if (!await userManagementService.VerifyTwoFactorCodeAsync(user, twoFactorCodeDto.Code))
        {
            audit.Log(SecurityEvent.TwoFactorVerificationFailed, success: false, userId: user.Id, detail: "regenerate recovery codes");
            return TypedResults.BadRequest();
        }

        var recoveryCodes = await userManagementService.GenerateNewRecoveryCodesAsync(user);
        if (recoveryCodes is null)
            return TypedResults.BadRequest();

        audit.Log(SecurityEvent.RecoveryCodesRegenerated, userId: user.Id);

        return TypedResults.Ok(new RecoveryCodesDto
        {
            RecoveryCodes = recoveryCodes
        });
    }

    // Passkey Handlers

    private static async Task<Results<Ok<List<PasskeyDto>>, ForbidHttpResult, UnauthorizedHttpResult>> GetPasskeysHandler(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        var passkeys = await userManager.GetPasskeysAsync(user);
        return TypedResults.Ok(passkeys
            .OrderBy(p => p.CreatedAt)
            .Select(ToPasskeyDto)
            .ToList());
    }

    private static async Task<Results<Ok<PasskeyOptionsDto>, ForbidHttpResult, UnauthorizedHttpResult, BadRequest>> PasskeyCreationOptionsHandler(
        [FromBody] PasskeyCreationOptionsRequestDto dto,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        SecurityAudit audit)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        // A passkey is a lasting way in, so a hijacked session must not be able to add one.
        if (!await userManager.CheckPasswordAsync(user, dto.CurrentPassword))
        {
            audit.Log(SecurityEvent.TwoFactorVerificationFailed, success: false, userId: user.Id,
                detail: "passkey registration: wrong password");
            return TypedResults.BadRequest();
        }

        var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity
        {
            Id = user.Id,
            Name = user.Email ?? user.UserName ?? user.Id,
            DisplayName = user.UserName ?? user.Email ?? user.Id,
        });

        return TypedResults.Ok(new PasskeyOptionsDto { OptionsJson = optionsJson });
    }

    private static async Task<Results<Ok<PasskeyDto>, ForbidHttpResult, UnauthorizedHttpResult, BadRequest>> AddPasskeyHandler(
        [FromBody] AddPasskeyDto dto,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        SecurityAudit audit)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        // Only succeeds for options issued by PasskeyCreationOptionsHandler to this browser (state cookie).
        PasskeyAttestationResult attestation;
        try
        {
            attestation = await signInManager.PerformPasskeyAttestationAsync(dto.CredentialJson);
        }
        catch (InvalidOperationException)
        {
            // No (or expired) attestation state cookie.
            audit.Log(SecurityEvent.TwoFactorVerificationFailed, success: false, userId: user.Id,
                detail: "passkey registration: no registration in progress");
            return TypedResults.BadRequest();
        }
        if (!attestation.Succeeded || attestation.UserEntity.Id != user.Id)
        {
            audit.Log(SecurityEvent.TwoFactorVerificationFailed, success: false, userId: user.Id,
                detail: $"passkey registration: {attestation.Failure?.Message ?? "user mismatch"}");
            return TypedResults.BadRequest();
        }

        var passkey = attestation.Passkey;
        var name = dto.Name?.Trim();
        passkey.Name = string.IsNullOrEmpty(name) ? "Passkey" : name[..Math.Min(name.Length, 100)];

        var result = await userManager.AddOrUpdatePasskeyAsync(user, passkey);
        if (!result.Succeeded)
        {
            audit.Log(SecurityEvent.PasskeyAdded, success: false, userId: user.Id,
                detail: string.Join(", ", result.Errors.Select(e => e.Code)));
            return TypedResults.BadRequest();
        }

        audit.Log(SecurityEvent.PasskeyAdded, userId: user.Id, detail: $"'{passkey.Name}'");

        // Adding a passkey may rotate the security stamp; re-issue the cookie so this session stays valid.
        await signInManager.RefreshSignInAsync(user);

        return TypedResults.Ok(ToPasskeyDto(passkey));
    }

    private static async Task<Results<Ok, ForbidHttpResult, UnauthorizedHttpResult, NotFound, BadRequest<string>>> RemovePasskeyHandler(
        [FromRoute] string id,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TwoFactorPolicy twoFactorPolicy,
        SecurityAudit audit)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        byte[] credentialId;
        try
        {
            credentialId = WebEncoders.Base64UrlDecode(id);
        }
        catch (FormatException)
        {
            return TypedResults.NotFound();
        }

        var passkey = await userManager.GetPasskeyAsync(user, credentialId);
        if (passkey is null)
            return TypedResults.NotFound();

        if (!user.TwoFactorEnabled && await twoFactorPolicy.IsRequiredAsync(user) &&
            (await userManager.GetPasskeysAsync(user)).Count <= 1)
            return TypedResults.BadRequest("Two-factor authentication is required for your account. Enable an authenticator app or add another passkey first.");

        var result = await userManager.RemovePasskeyAsync(user, credentialId);
        if (!result.Succeeded)
            return TypedResults.BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));

        audit.Log(SecurityEvent.PasskeyRemoved, userId: user.Id, detail: $"'{passkey.Name}'");
        await signInManager.RefreshSignInAsync(user);

        return TypedResults.Ok();
    }

    private static PasskeyDto ToPasskeyDto(UserPasskeyInfo passkey) => new()
    {
        Id = WebEncoders.Base64UrlEncode(passkey.CredentialId),
        Name = passkey.Name,
        CreatedAt = passkey.CreatedAt,
        IsBackedUp = passkey.IsBackedUp,
    };

}
