using Identity.App.Data;
using Identity.App.EndPoints.External.Models;
using Identity.App.EndPoints.Users;
using Identity.App.Models;
using Identity.App.Models.Consts;
using Identity.App.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using UpdateUserDto = Identity.App.EndPoints.External.Models.UpdateUserDto;
using UserDto = Identity.App.EndPoints.Users.Models.UserDto;

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

        // Application role requirements endpoints
        group.MapGet("applications", GetApplicationsWithRolesHandler)
            .WithName("Admin/GetApplicationsWithRoles");

        group.MapGet("applications/{clientId}/roles", GetApplicationRequiredRolesHandler)
            .WithName("Admin/GetApplicationRequiredRoles");

        group.MapPut("applications/{clientId}/roles", UpdateApplicationRequiredRolesHandler)
            .WithName("Admin/UpdateApplicationRequiredRoles");

        // All roles endpoint
        group.MapGet("roles", GetAllRolesHandler)
            .WithName("Admin/GetAllRoles");

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

    private static async Task<Results<Ok, BadRequest, NotFound>> AddUserToRoleByIdHandler(
        [FromRoute] string userId,
        [FromRoute] string roleName,
        UserManagementService userManagementService)
    {
        var result = await userManagementService.AddUserToRoleByIdAsync(userId, roleName);
        if (!result)
            return TypedResults.BadRequest();
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, BadRequest, NotFound>> AddUserToRoleByEmailHandler(
        [FromRoute] string email,
        [FromRoute] string roleName,
        UserManagementService userManagementService)
    {
        var result = await userManagementService.AddUserToRoleByEmailAsync(email, roleName);
        if (!result)
            return TypedResults.BadRequest();
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, BadRequest, NotFound>> RemoveUserFromRoleByIdHandler(
        [FromRoute] string userId,
        [FromRoute] string roleName,
        UserManagementService userManagementService)
    {
        var result = await userManagementService.RemoveUserFromRoleByIdAsync(userId, roleName);
        if (!result)
            return TypedResults.BadRequest();
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, BadRequest, NotFound>> RemoveUserFromRoleByEmailHandler(
        [FromRoute] string email,
        [FromRoute] string roleName,
        UserManagementService userManagementService)
    {
        var result = await userManagementService.RemoveUserFromRoleByEmailAsync(email, roleName);
        if (!result)
            return TypedResults.BadRequest();
        return TypedResults.Ok();
    }

    // Application Role Requirements Handlers

    private static async Task<Ok<List<ApplicationRoleInfo>>> GetApplicationsWithRolesHandler(
        IOpenIddictApplicationManager applicationManager)
    {
        var applications = new List<ApplicationRoleInfo>();
        
        await foreach (var app in applicationManager.ListAsync())
        {
            var clientId = await applicationManager.GetClientIdAsync(app);
            var displayName = await applicationManager.GetDisplayNameAsync(app);
            var properties = await applicationManager.GetPropertiesAsync(app);
            
            var requiredRoles = new List<string>();
            if (properties.TryGetValue("RequiredRoles", out var rolesElement))
            {
                if (rolesElement is System.Text.Json.JsonElement jsonElement && 
                    jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    requiredRoles = jsonElement.EnumerateArray()
                        .Where(e => e.ValueKind == System.Text.Json.JsonValueKind.String)
                        .Select(e => e.GetString() ?? "")
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();
                }
            }

            applications.Add(new ApplicationRoleInfo
            {
                ClientId = clientId,
                DisplayName = displayName,
                RequiredRoles = requiredRoles
            });
        }

        return TypedResults.Ok(applications);
    }

    private static async Task<Results<Ok<List<string>>, NotFound>> GetApplicationRequiredRolesHandler(
        [FromRoute] string clientId,
        IOpenIddictApplicationManager applicationManager)
    {
        var app = await applicationManager.FindByClientIdAsync(clientId);
        if (app == null)
            return TypedResults.NotFound();

        var properties = await applicationManager.GetPropertiesAsync(app);
        var requiredRoles = new List<string>();
        
        if (properties.TryGetValue("RequiredRoles", out var rolesElement))
        {
            if (rolesElement is System.Text.Json.JsonElement jsonElement && 
                jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                requiredRoles = jsonElement.EnumerateArray()
                    .Where(e => e.ValueKind == System.Text.Json.JsonValueKind.String)
                    .Select(e => e.GetString() ?? "")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }
        }

        return TypedResults.Ok(requiredRoles);
    }

    private static async Task<Results<Ok, NotFound>> UpdateApplicationRequiredRolesHandler(
        [FromRoute] string clientId,
        [FromBody] List<string> requiredRoles,
        IOpenIddictApplicationManager applicationManager)
    {
        var app = await applicationManager.FindByClientIdAsync(clientId);
        if (app == null)
            return TypedResults.NotFound();

        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, app);
        
        descriptor.Properties["RequiredRoles"] = System.Text.Json.JsonSerializer.SerializeToElement(
            requiredRoles ?? new List<string>());

        await applicationManager.UpdateAsync(app, descriptor);

        return TypedResults.Ok();
    }

    private static async Task<Ok<List<RoleInfo>>> GetAllRolesHandler(
        RoleManager<IdentityRole> roleManager)
    {
        var roles = roleManager.Roles
            .Select(r => new RoleInfo 
            { 
                Id = r.Id, 
                Name = r.Name 
            })
            .ToList();

        return TypedResults.Ok(roles);
    }

    
}

// DTOs
public class ApplicationRoleInfo
{
    public string ClientId { get; set; }
    public string DisplayName { get; set; }
    public List<string> RequiredRoles { get; set; }
}

public class RoleInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
}
