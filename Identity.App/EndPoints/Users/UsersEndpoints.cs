using Identity.App.Data;
using Identity.App.EndPoints.Users.Models;
using Identity.App.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
            .WithName("Enable2fa");

        group.MapPost("me/2fa/disable", Disable2faHandler)
            .WithName("Disable2fa");

        group.MapPost("me/2fa/recovery-codes", GenerateRecoveryCodesHandler)
            .WithName("GenerateRecoveryCodes");

        return app;
    }

    private static async Task<Results<Ok<UserDto?>, ForbidHttpResult>> CurrentUserHandler(HttpContext httpContext, UserManager<ApplicationUser> userManager)
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
            UserName = user.UserName,
            TwoFactorEnabled = user.TwoFactorEnabled,
        });
    }

    private static async Task<Results<Ok<UserDto>, ForbidHttpResult, UnauthorizedHttpResult>> UpdateUserHandler(
    [FromBody] UpdateUserDto updateUserDto,
    HttpContext httpContext,
    UserManager<ApplicationUser> userManager)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        if (!string.IsNullOrWhiteSpace(updateUserDto.Username))
        {
            user.UserName = updateUserDto.Username;
        }

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            UserName = user.UserName ?? "",
            TwoFactorEnabled = user.TwoFactorEnabled,
        });
    }


    private static async Task<Results<Ok, ForbidHttpResult, UnauthorizedHttpResult>> UpdatePasswordHandler(
        [FromBody] UpdatePasswordDto updatePasswordDto,
        HttpContext httpContext, 
        UserManager<ApplicationUser> userManager)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        var resetPasswordResult = await userManager.ChangePasswordAsync(user, updatePasswordDto.CurrentPassword, updatePasswordDto.NewPassword);
        if (!resetPasswordResult.Succeeded)
        {
            return TypedResults.Unauthorized();
        }

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
        UserManagementService userManagementService)
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
            return TypedResults.BadRequest();

        return TypedResults.Ok(new RecoveryCodesDto
        {
            RecoveryCodes = recoveryCodes
        });
    }

    private static async Task<Results<Ok, ForbidHttpResult, UnauthorizedHttpResult, BadRequest>> Disable2faHandler(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        UserManagementService userManagementService)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        if (!user.TwoFactorEnabled)
            return TypedResults.BadRequest();

        var success = await userManagementService.DisableTwoFactorAsync(user);
        if (!success)
            return TypedResults.BadRequest();

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<RecoveryCodesDto>, ForbidHttpResult, UnauthorizedHttpResult, BadRequest>> GenerateRecoveryCodesHandler(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        UserManagementService userManagementService)
    {
        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return TypedResults.Unauthorized();

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
            return TypedResults.Forbid();

        if (!user.TwoFactorEnabled)
            return TypedResults.BadRequest();

        var recoveryCodes = await userManagementService.GenerateNewRecoveryCodesAsync(user);
        if (recoveryCodes is null)
            return TypedResults.BadRequest();

        return TypedResults.Ok(new RecoveryCodesDto
        {
            RecoveryCodes = recoveryCodes
        });
    }

}
