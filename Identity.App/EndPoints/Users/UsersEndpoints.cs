using Identity.App.Data;
using Identity.App.EndPoints.Users.Models;
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

        if (string.IsNullOrWhiteSpace(updateUserDto.Username))
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

}
