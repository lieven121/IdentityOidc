using Identity.App.EndPoints.External.Models;
using Identity.App.EndPoints.Users.Models;
using Identity.App.Models;
using Identity.App.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using UpdateUserDto = Identity.App.EndPoints.External.Models.UpdateUserDto;

namespace Identity.App.EndPoints.Users;

public static class UsersAdminEndpointsHelper
{
    public static RouteGroupBuilder RegisterUserAdminEndpoints(this RouteGroupBuilder group, string prefix)
    {
        group
            .RegisterUserAdminEndpointsRead(prefix)
            .RegisterUserAdminEndpointsCreate(prefix)
            .RegisterUserAdminEndpointsById(prefix)
            .RegisterUserAdminEndpointsByEmail(prefix);

        return group;
    }

    public static RouteGroupBuilder RegisterUserAdminEndpointsRead(this RouteGroupBuilder group, string prefix)
    {
        group.MapGet("users", GetUsersHandler)
           .WithName($"{prefix}GetUsers");

        group.MapPost("users/search", GetUsersHandlerSearch)
            .WithName($"{prefix}GetUsersSearch");

        return group;
    }

    public static RouteGroupBuilder RegisterUserAdminEndpointsCreate(this RouteGroupBuilder group, string prefix)
    {
        group.MapPost("users", CreateUserHandler)
            .WithName($"{prefix}CreateUser");

        return group;
    }


    public static RouteGroupBuilder RegisterUserAdminEndpointsById(this RouteGroupBuilder group, string prefix)
    {
        group.MapGet("users/{id}", GetUserById)
            .WithName($"{prefix}GetUserById");

        group.MapPost("users/{userId}", UpdateUserHandler)
            .WithName($"{prefix}UpdateUser");

        group.MapPost("users/{userId}/password", ResetPasswordHandler)
            .WithName($"{prefix}ResetPassword");

        return group;
    }


    public static RouteGroupBuilder RegisterUserAdminEndpointsByEmail(this RouteGroupBuilder group, string prefix)
    {
       
        group.MapGet("users/email/{email}", GetUserByEmail)
            .WithName($"{prefix}GetUserByEmail");

        group.MapPost("users/email/{email}", UpdateUserByEmailHandler)
            .WithName($"{prefix}UpdateUserByEmail");

        group.MapPost("users/email/{email}/password", ResetPasswordByEmailHandler)
            .WithName($"{prefix}ResetPasswordByEmail");

        return group;
    }

    private static async Task<Results<Ok<UserDto?>, NotFound>> GetUserByEmail(
        string email,
        HttpContext context,
        UserManagementService userManagementService,
        bool returnNullWhenNotFound = false)
    {
        return await userManagementService.GetUserByEmail(email, returnNullWhenNotFound);
    }

    private static async Task<Results<Ok<UserDto?>, NotFound>> GetUserById(
        string id,
        HttpContext context,
        UserManagementService userManagementService,
        bool returnNullWhenNotFound = false)
    {
        return await userManagementService.GetUserById(id, returnNullWhenNotFound);
    }

    private static async Task<Results<Ok<ResultPage<UserDto>>, ForbidHttpResult>> GetUsersHandler(
        HttpContext httpContext,
        UserManagementService userManagementService)
    {
        return await GetUsersHandlerSearch(null, httpContext, userManagementService);
    }

    private static async Task<Results<Ok<ResultPage<UserDto>>, ForbidHttpResult>> GetUsersHandlerSearch(
        [FromBody] UsersFilter? filter,
        HttpContext httpContext,
        UserManagementService userManagementService)
    {
        return await userManagementService.GetUsers(filter);
    }

    private static async Task<Results<Ok<UserDto>, Conflict>> CreateUserHandler(
        [FromBody] CreateUserDto createUserDto,
        HttpContext context,
        UserManagementService userManagementService)
    {
        return await userManagementService.CreateUser(createUserDto);
    }

    private static async Task<Results<Ok<UserDto>, Conflict>> UpdateUserHandler(
        [FromRoute] string userId,
        [FromBody] UpdateUserDto updateUserDto,
        HttpContext context,
        UserManagementService userManagementService)
    {
        return await userManagementService.UpdateUserById(userId, updateUserDto);
    }

    private static async Task<Results<Ok<UserDto>, Conflict>> UpdateUserByEmailHandler(
        [FromRoute] string email,
        [FromBody] UpdateUserDto updateUserDto,
        HttpContext context,
        UserManagementService userManagementService)
    {
        return await userManagementService.UpdateUserByEmail(email, updateUserDto);
    }

    private static async Task<Results<Ok<UserDto>, BadRequest, Conflict>> ResetPasswordHandler(
        [FromRoute] string userId,
        [FromBody] ResetPasswordDto resetPasswordDto,
        HttpContext context,
        UserManagementService userManagementService)
    {
        return await userManagementService.ResetPasswordById(userId, resetPasswordDto);
    }

    private static async Task<Results<Ok<UserDto>, BadRequest, Conflict>> ResetPasswordByEmailHandler(
        [FromRoute] string email,
        [FromBody] ResetPasswordDto resetPasswordDto,
        HttpContext context,
        UserManagementService userManagementService)
    {
        return await userManagementService.ResetPasswordByEmail(email, resetPasswordDto);
    }
}
