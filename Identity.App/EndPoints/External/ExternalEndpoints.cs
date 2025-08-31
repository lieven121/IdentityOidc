using Identity.App.Data;
using Identity.App.EndPoints.External.Models;
using Identity.App.EndPoints.Users.Models;
using Identity.App.Helpers;
using Identity.App.Models;
using Identity.App.Models.Consts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using UpdateUserDto = Identity.App.EndPoints.External.Models.UpdateUserDto;

namespace Identity.App.EndPoints.External;

public static class ExternalEndpoints
{
    public static IEndpointRouteBuilder MapExternalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("External")
            .WithTags("External")
            .WithName("External")

            .RequireAuthorization(policy =>
            {
                policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("oi_scp", Scopes.Admin);
            }
            );


        group.MapGet("users", GetUsersHandler)
            .WithName("External/GetUsers");

        group.MapPost("users/search", GetUsersHandlerSearch)
            .WithName("External/GetUsersSearch");

        group.MapGet("users/email/{email}", GetUserByEmail)
            .WithName("External/GetUserByEmail");

        group.MapGet("users/{id}", GetUserById)
            .WithName("External/GetUserById");

        group.MapPost("users", CreateUserHandler)
            .WithName("External/CreateUser");

        group.MapPost("users/{userId}", UpdateUserHandler)
            .WithName("External/UpdateUser");

        group.MapPost("users/{userId}/password", ResetPasswordHandler)
            .WithName("External/ResetPassword");

        group.MapPost("users/email/{email}", UpdateUserByEmailHandler)
            .WithName("External/UpdateUserByEmail");

        group.MapPost("users/email/{email}/password", ResetPasswordByEmailHandler)
            .WithName("External/ResetPasswordByEmail");
        //reset password

        return app;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="email"></param>
    /// <param name="returnNullWhenNotFound">Return null 200 Ok instead of 404</param>
    /// <returns></returns>
    private static async Task<Results<Ok<UserDto?>, NotFound>> GetUserByEmail(string email,
        HttpContext context,
        UserManager<ApplicationUser> userManager,
        bool returnNullWhenNotFound = false
        )
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            if (returnNullWhenNotFound)
                return TypedResults.Ok<UserDto?>(null);
            return TypedResults.NotFound();
        }
        return TypedResults.Ok<UserDto?>(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            UserName = user.UserName ?? "",
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="returnNullWhenNotFound">Return null 200 Ok instead of 404</param>
    /// <returns></returns>
    private static async Task<Results<Ok<UserDto?>, NotFound>> GetUserById(string id,
    HttpContext context,
    UserManager<ApplicationUser> userManager,
    bool returnNullWhenNotFound = false
    )
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            if (returnNullWhenNotFound)
                return TypedResults.Ok<UserDto?>(null);
            return TypedResults.NotFound();
        }
        return TypedResults.Ok<UserDto?>(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            UserName = user.UserName ?? "",
        });
    }


    private static async Task<Results<Ok<ResultPage<UserDto>>, ForbidHttpResult>> GetUsersHandler(
    HttpContext httpContext,
    UserManager<ApplicationUser> userManager
    )
    {
        return await GetUsersHandlerSearch(
            null,
            httpContext,
            userManager
        );
    }

    private static async Task<Results<Ok<ResultPage<UserDto>>, ForbidHttpResult>> GetUsersHandlerSearch(
        [FromBody] UsersFilter? filter,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager
        )
    {
        var qry = userManager.Users.AsQueryable();

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Username))
            {
                qry = qry.Where(q => q.UserName != null && q.UserName.Contains(filter.Username));
            }

            if (!string.IsNullOrWhiteSpace(filter.Email))
            {
                qry = qry.Where(q => q.Email != null && q.Email.Contains(filter.Email));
            }

        } else
        {
            qry = qry.OrderByDescending(x => x.Id);
        }

            var count = await qry.CountAsync();

        var users = await qry
            .Skip(filter?.Page ?? 0 * (filter?.PageSize ?? 50))
            .Take(filter?.PageSize ?? 50)
            .Select(x => new UserDto
            {
                Id = x.Id,
                Email = x.Email ?? "",
                UserName = x.UserName ?? "",
            })
            .ToListAsync();

        return TypedResults.Ok(new ResultPage<UserDto>
        {
            Items = users,
            TotalCount = count,
        });
    }





    private static async Task<Results<Ok<UserDto>, Conflict>> CreateUserHandler(
    [FromBody] CreateUserDto createUserDto,
    HttpContext context,
    UserManager<ApplicationUser> userManager)
    {


        var result = await userManager.FindByEmailAsync(createUserDto.Email);
        if (result != null)
            return TypedResults.Conflict();

        var user = new ApplicationUser
        {
            Email = createUserDto.Email,
            EmailConfirmed = createUserDto.EmailConfirmed,
            UserName = createUserDto.Username,
        };


        await userManager.CreateAsync(user);
        
        if(!string.IsNullOrWhiteSpace(createUserDto.Password))
            await userManager.AddPasswordAsync(user, createUserDto.Password);
        else
        {
            //add password and send email?
        }

            return TypedResults.Ok(new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
            });
    }



    private static async Task<Results<Ok<UserDto>, Conflict>> UpdateUserHandler(
        [FromRoute] string userId,
        [FromBody] UpdateUserDto updateUserDto,
        HttpContext context,
        UserManager<ApplicationUser> userManager)
    {
        return await UpdateUser(
            await userManager.FindByIdAsync(userId),
            updateUserDto,
            context,
            userManager
        );
    }

    private static async Task<Results<Ok<UserDto>, Conflict>> UpdateUserByEmailHandler(
        [FromRoute] string email,
        [FromBody] UpdateUserDto updateUserDto,
        HttpContext context,
        UserManager<ApplicationUser> userManager)
    {
        return await UpdateUser(
            await userManager.FindByEmailAsync(email),
            updateUserDto,
            context,
            userManager
        );
    }


    private static async Task<Results<Ok<UserDto>, Conflict>> UpdateUser(
        ApplicationUser? user,
        UpdateUserDto updateUserDto,
        HttpContext context,
        UserManager<ApplicationUser> userManager)
    {
        if (user is null)
        {
            return TypedResults.Conflict();
        }

        if (!string.IsNullOrWhiteSpace(updateUserDto.Username))
        {
            user.UserName = updateUserDto.Username;
        }

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return TypedResults.Conflict();
        }

        return TypedResults.Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            UserName = user.UserName ?? "",
        });

    }

    private static async Task<Results<Ok<UserDto>, BadRequest, Conflict>> ResetPasswordHandler(
        [FromRoute] string userId,
        [FromBody] ResetPasswordDto resetPasswordDto,
        HttpContext context,
        UserManager<ApplicationUser> userManager)
    {
        return await ResetPassword(
            await userManager.FindByIdAsync(userId),
            resetPasswordDto,
            context,
            userManager
        );
    }

    private static async Task<Results<Ok<UserDto>, BadRequest, Conflict>> ResetPasswordByEmailHandler(
        [FromRoute] string email,
        [FromBody] ResetPasswordDto resetPasswordDto,
        HttpContext context,
        UserManager<ApplicationUser> userManager)
    {
        return await ResetPassword(
            await userManager.FindByEmailAsync(email),
            resetPasswordDto,
            context,
            userManager
        );
    }

    private static async Task<Results<Ok<UserDto>, BadRequest, Conflict>> ResetPassword(
    ApplicationUser? user,
    ResetPasswordDto resetPasswordDto,
    HttpContext context,
    UserManager<ApplicationUser> userManager)
    {
        if (user is null)
        {
            return TypedResults.Conflict();
        }
        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        var password = !string.IsNullOrEmpty(resetPasswordDto?.NewPassword) ? resetPasswordDto.NewPassword : PasswordGenerator.QuicklyGeneratePassword(16, 24);

        var result = await userManager.ResetPasswordAsync(user, token, password);
        if (!result.Succeeded)
        {
            return TypedResults.BadRequest();
        }
        return TypedResults.Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            UserName = user.UserName ?? "",
        });
    }

}
