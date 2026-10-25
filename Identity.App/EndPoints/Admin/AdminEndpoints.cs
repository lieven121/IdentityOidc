using Identity.App.EndPoints.Admin.Models;
using Identity.App.EndPoints.Users;
using Identity.App.Models.Consts;
using Identity.App.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Identity.App.EndPoints.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("Admin")
            .WithTags("Admin")
            .WithName("Admin")

            .RequireAuthorization(policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(Roles.Admin);
                policy.RequireTwoFactorCompliance();
            }
            );

        var prefix = "Admin/";

        group
            .RegisterUserAdminEndpointsRead(prefix)
            .RegisterUserAdminEndpointsCreate(prefix)
            .RegisterUserAdminEndpointsById(prefix);

        // Role management endpoints
        group.MapGet("users/{userId}/roles", GetUserRolesByIdHandler)
            .WithName("Admin/GetUserRolesById");

        group.MapGet("users/email/{email}/roles", GetUserRolesByEmailHandler)
            .WithName("Admin/GetUserRolesByEmail");

        group.MapPost("users/{userId}/roles/{roleName}", AddUserToRoleByIdHandler)
            .WithName("Admin/AddUserToRoleById");

        group.MapPost("users/email/{email}/roles/{roleName}", AddUserToRoleByEmailHandler)
            .WithName("Admin/AddUserToRoleByEmail");

        group.MapDelete("users/{userId}/roles/{roleName}", RemoveUserFromRoleByIdHandler)
            .WithName("Admin/RemoveUserFromRoleById");

        group.MapDelete("users/email/{email}/roles/{roleName}", RemoveUserFromRoleByEmailHandler)
            .WithName("Admin/RemoveUserFromRoleByEmail");

        // Applications
        group.MapGet("applications", GetApplicationsHandler)
            .WithName("Admin/GetApplications");

        group.MapPost("applications", CreateApplicationHandler)
            .WithName("Admin/CreateApplication");

        group.MapGet("applications/{clientId}", GetApplicationHandler)
            .WithName("Admin/GetApplication");

        group.MapPut("applications/{clientId}", UpdateApplicationHandler)
            .WithName("Admin/UpdateApplication");

        group.MapDelete("applications/{clientId}", DeleteApplicationHandler)
            .WithName("Admin/DeleteApplication");

        group.MapPut("applications/{clientId}/secret", SetApplicationSecretHandler)
            .WithName("Admin/SetApplicationSecret");

        group.MapGet("applications/{clientId}/roles", GetApplicationRequiredRolesHandler)
            .WithName("Admin/GetApplicationRequiredRoles");

        group.MapPut("applications/{clientId}/roles", UpdateApplicationRequiredRolesHandler)
            .WithName("Admin/UpdateApplicationRequiredRoles");

        group.MapGet("scopes", GetScopesHandler)
            .WithName("Admin/GetScopes");

        // Roles
        group.MapGet("roles", GetAllRolesHandler)
            .WithName("Admin/GetAllRoles");

        group.MapPost("roles", CreateRoleHandler)
            .WithName("Admin/CreateRole");

        group.MapPut("roles/{name}", RenameRoleHandler)
            .WithName("Admin/RenameRole");

        group.MapDelete("roles/{name}", DeleteRoleHandler)
            .WithName("Admin/DeleteRole");

        group.MapGet("roles/{name}/users", GetRoleMembersHandler)
            .WithName("Admin/GetRoleMembers");

        // Two-Factor Authentication reset endpoints
        group.MapPost("users/{userId}/2fa/reset", ResetUser2faByIdHandler)
            .WithName("Admin/ResetUser2faById");

        group.MapPost("users/email/{email}/2fa/reset", ResetUser2faByEmailHandler)
            .WithName("Admin/ResetUser2faByEmail");

        return app;
    }

    // User Role Management Handlers

    private static async Task<Results<Ok<IList<string>>, NotFound>> GetUserRolesByIdHandler(
        [FromRoute] string userId,
        UserManagementService userManagementService)
    {
        var roles = await userManagementService.GetUserRolesByIdAsync(userId);
        if (roles == null || !roles.Any())
            return TypedResults.NotFound();
        return TypedResults.Ok(roles);
    }

    private static async Task<Results<Ok<IList<string>>, NotFound>> GetUserRolesByEmailHandler(
        [FromRoute] string email,
        UserManagementService userManagementService)
    {
        var roles = await userManagementService.GetUserRolesByEmailAsync(email);
        if (roles == null || !roles.Any())
            return TypedResults.NotFound();
        return TypedResults.Ok(roles);
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound, Conflict<string>>> AddUserToRoleByIdHandler(
        [FromRoute] string userId,
        [FromRoute] string roleName,
        UserManagementService userManagementService)
        => ToResult(await userManagementService.AddUserToRoleByIdAsync(userId, roleName));

    private static async Task<Results<Ok, BadRequest<string>, NotFound, Conflict<string>>> AddUserToRoleByEmailHandler(
        [FromRoute] string email,
        [FromRoute] string roleName,
        UserManagementService userManagementService)
        => ToResult(await userManagementService.AddUserToRoleByEmailAsync(email, roleName));

    private static async Task<Results<Ok, BadRequest<string>, NotFound, Conflict<string>>> RemoveUserFromRoleByIdHandler(
        [FromRoute] string userId,
        [FromRoute] string roleName,
        UserManagementService userManagementService)
        => ToResult(await userManagementService.RemoveUserFromRoleByIdAsync(userId, roleName));

    private static async Task<Results<Ok, BadRequest<string>, NotFound, Conflict<string>>> RemoveUserFromRoleByEmailHandler(
        [FromRoute] string email,
        [FromRoute] string roleName,
        UserManagementService userManagementService)
        => ToResult(await userManagementService.RemoveUserFromRoleByEmailAsync(email, roleName));

    private static Results<Ok, BadRequest<string>, NotFound, Conflict<string>> ToResult(RoleMembershipResult result) => result switch
    {
        RoleMembershipResult.Ok => TypedResults.Ok(),
        RoleMembershipResult.UserNotFound => TypedResults.NotFound(),
        RoleMembershipResult.RoleNotFound => TypedResults.BadRequest("The role does not exist."),
        RoleMembershipResult.ConfigManaged => TypedResults.Conflict("This user's roles are managed by configuration and can't be changed here."),
        RoleMembershipResult.OwnAdminRole => TypedResults.Conflict("You can't remove the Admin role from yourself."),
        _ => TypedResults.BadRequest("The role membership could not be changed."),
    };

    // Application Handlers

    private static async Task<Ok<List<ApplicationDto>>> GetApplicationsHandler(
        ApplicationManagementService applicationService)
        => TypedResults.Ok(await applicationService.ListAsync());

    private static async Task<Results<Ok<ApplicationDto>, NotFound>> GetApplicationHandler(
        [FromRoute] string clientId,
        ApplicationManagementService applicationService)
    {
        var application = await applicationService.GetAsync(clientId);
        return application == null ? TypedResults.NotFound() : TypedResults.Ok(application);
    }

    private static async Task<Results<Ok<ApplicationSecretDto>, BadRequest<string>, Conflict<string>>> CreateApplicationHandler(
        [FromBody] SaveApplicationDto application,
        ApplicationManagementService applicationService)
        => await applicationService.CreateAsync(application);

    private static async Task<Results<Ok<ApplicationSecretDto>, BadRequest<string>, Conflict<string>, NotFound>> UpdateApplicationHandler(
        [FromRoute] string clientId,
        [FromBody] SaveApplicationDto application,
        ApplicationManagementService applicationService)
        => await applicationService.UpdateAsync(clientId, application);

    private static async Task<Results<Ok, Conflict<string>, NotFound>> DeleteApplicationHandler(
        [FromRoute] string clientId,
        ApplicationManagementService applicationService)
        => await applicationService.DeleteAsync(clientId);

    private static async Task<Results<Ok<ApplicationSecretDto>, BadRequest<string>, Conflict<string>, NotFound>> SetApplicationSecretHandler(
        [FromRoute] string clientId,
        [FromBody] SetClientSecretDto secret,
        ApplicationManagementService applicationService)
        => await applicationService.SetSecretAsync(clientId, secret);

    private static async Task<Results<Ok<List<string>>, NotFound>> GetApplicationRequiredRolesHandler(
        [FromRoute] string clientId,
        ApplicationManagementService applicationService)
    {
        var application = await applicationService.GetAsync(clientId);
        return application == null ? TypedResults.NotFound() : TypedResults.Ok(application.RequiredRoles);
    }

    private static async Task<Results<Ok, BadRequest<string>, Conflict<string>, NotFound>> UpdateApplicationRequiredRolesHandler(
        [FromRoute] string clientId,
        [FromBody] List<string> requiredRoles,
        ApplicationManagementService applicationService)
        => await applicationService.UpdateRequiredRolesAsync(clientId, requiredRoles);

    private static async Task<Ok<List<string>>> GetScopesHandler(
        ApplicationManagementService applicationService)
        => TypedResults.Ok(await applicationService.ListScopesAsync());

    // Role Handlers

    private static async Task<Ok<List<RoleInfo>>> GetAllRolesHandler(
        RoleManagementService roleService)
        => TypedResults.Ok(await roleService.ListAsync());

    private static async Task<Results<Ok<RoleInfo>, BadRequest<string>, Conflict<string>>> CreateRoleHandler(
        [FromBody] RoleNameDto role,
        RoleManagementService roleService)
        => await roleService.CreateAsync(role);

    private static async Task<Results<Ok<RoleInfo>, BadRequest<string>, Conflict<string>, NotFound>> RenameRoleHandler(
        [FromRoute] string name,
        [FromBody] RoleNameDto role,
        RoleManagementService roleService)
        => await roleService.RenameAsync(name, role);

    private static async Task<Results<Ok, Conflict<string>, NotFound>> DeleteRoleHandler(
        [FromRoute] string name,
        RoleManagementService roleService)
        => await roleService.DeleteAsync(name);

    private static async Task<Results<Ok<List<RoleMemberDto>>, NotFound>> GetRoleMembersHandler(
        [FromRoute] string name,
        RoleManagementService roleService)
        => await roleService.GetMembersAsync(name);

    // Two-Factor Authentication Reset Handlers

    private static async Task<Results<Ok, NotFound>> ResetUser2faByIdHandler(
        [FromRoute] string userId,
        UserManagementService userManagementService)
    {
        var result = await userManagementService.ResetTwoFactorByIdAsync(userId);
        if (!result)
            return TypedResults.NotFound();
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, NotFound>> ResetUser2faByEmailHandler(
        [FromRoute] string email,
        UserManagementService userManagementService)
    {
        var result = await userManagementService.ResetTwoFactorByEmailAsync(email);
        if (!result)
            return TypedResults.NotFound();
        return TypedResults.Ok();
    }
}
