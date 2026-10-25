using Identity.App.Data;
using Identity.App.EndPoints.Admin.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Identity.App.Services;

/// <summary>
/// Admin management of roles. Roles declared in configuration (and Admin) can't be renamed or deleted,
/// and roles still referenced by an application or by 2FA enforcement can't be deleted.
/// </summary>
[RegisterScoped]
public partial class RoleManagementService(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IOpenIddictApplicationManager applicationManager,
    ConfigEntries configEntries,
    SecurityAudit audit)
{
    [GeneratedRegex("^[A-Za-z0-9 ._-]{1,64}$")]
    private static partial Regex RoleNamePattern();

    public async Task<List<RoleInfo>> ListAsync()
    {
        var counts = await dbContext.UserRoles
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count);

        var roles = await roleManager.Roles.ToListAsync();
        return roles
            .Select(r => new RoleInfo
            {
                Id = r.Id,
                Name = r.Name ?? "",
                UserCount = counts.GetValueOrDefault(r.Id),
                IsConfigManaged = configEntries.IsConfigRole(r.Name),
            })
            .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<Results<Ok<List<RoleMemberDto>>, NotFound>> GetMembersAsync(string name)
    {
        var role = await roleManager.FindByNameAsync(name);
        if (role?.Name == null)
            return TypedResults.NotFound();

        var users = await userManager.GetUsersInRoleAsync(role.Name);
        return TypedResults.Ok(users
            .Select(u => new RoleMemberDto
            {
                Id = u.Id,
                Username = u.UserName,
                Email = u.Email,
                IsConfigManaged = configEntries.IsConfigUser(u.Email),
            })
            .OrderBy(u => u.Email, StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    public async Task<Results<Ok<RoleInfo>, BadRequest<string>, Conflict<string>>> CreateAsync(RoleNameDto dto)
    {
        var name = dto.Name?.Trim() ?? "";
        if (!RoleNamePattern().IsMatch(name))
            return TypedResults.BadRequest(InvalidNameMessage);
        if (await roleManager.RoleExistsAsync(name))
            return TypedResults.Conflict($"The role '{name}' already exists.");

        var role = new IdentityRole(name);
        var result = await roleManager.CreateAsync(role);
        audit.Log(SecurityEvent.RoleCreated, success: result.Succeeded, detail: $"role '{name}'");
        if (!result.Succeeded)
            return TypedResults.BadRequest(string.Join(" ", result.Errors.Select(e => e.Description)));

        return TypedResults.Ok(new RoleInfo { Id = role.Id, Name = name });
    }

    public async Task<Results<Ok<RoleInfo>, BadRequest<string>, Conflict<string>, NotFound>> RenameAsync(string name, RoleNameDto dto)
    {
        var role = await roleManager.FindByNameAsync(name);
        if (role?.Name == null)
            return TypedResults.NotFound();
        var oldName = role.Name;

        var newName = dto.Name?.Trim() ?? "";
        if (!RoleNamePattern().IsMatch(newName))
            return TypedResults.BadRequest(InvalidNameMessage);
        if (newName == oldName)
            return TypedResults.Ok(await ToInfoAsync(role));

        if (configEntries.IsConfigRole(oldName))
            return TypedResults.Conflict($"The role '{oldName}' is managed by configuration and can't be renamed.");
        if (configEntries.EnforceTwoFactorRoles.Contains(oldName, StringComparer.OrdinalIgnoreCase))
            return TypedResults.Conflict($"The role '{oldName}' is listed in OpenIddict:EnforceTwoFactorRoles; renaming it would turn off 2FA enforcement for it.");
        var configApps = configEntries.Applications
            .Where(a => (a.RequiredRoles ?? []).Contains(oldName, StringComparer.OrdinalIgnoreCase))
            .Select(a => a.ClientId)
            .ToList();
        if (configApps.Count > 0)
            return TypedResults.Conflict($"The role '{oldName}' is required by configured application(s) {string.Join(", ", configApps)} and can't be renamed.");

        var existing = await roleManager.FindByNameAsync(newName);
        if (existing != null && existing.Id != role.Id)
            return TypedResults.Conflict($"The role '{newName}' already exists.");

        await roleManager.SetRoleNameAsync(role, newName);
        var result = await roleManager.UpdateAsync(role);
        audit.Log(SecurityEvent.RoleRenamed, success: result.Succeeded, detail: $"role '{oldName}' to '{newName}'");
        if (!result.Succeeded)
            return TypedResults.BadRequest(string.Join(" ", result.Errors.Select(e => e.Description)));

        // Keep applications that require the role working under its new name.
        await foreach (var app in applicationManager.ListAsync())
        {
            var required = await ApplicationDescriptorBuilder.GetRequiredRolesAsync(applicationManager, app);
            if (!required.Contains(oldName, StringComparer.OrdinalIgnoreCase))
                continue;

            var descriptor = new OpenIddictApplicationDescriptor();
            await applicationManager.PopulateAsync(descriptor, app);
            descriptor.Properties[ApplicationDescriptorBuilder.RequiredRolesProperty] = JsonSerializer.SerializeToElement(
                required.Select(r => string.Equals(r, oldName, StringComparison.OrdinalIgnoreCase) ? newName : r).ToList());
            await applicationManager.UpdateAsync(app, descriptor);
        }

        return TypedResults.Ok(await ToInfoAsync(role));
    }

    public async Task<Results<Ok, Conflict<string>, NotFound>> DeleteAsync(string name)
    {
        var role = await roleManager.FindByNameAsync(name);
        if (role?.Name == null)
            return TypedResults.NotFound();

        if (configEntries.IsConfigRole(role.Name))
            return TypedResults.Conflict($"The role '{role.Name}' is managed by configuration and can't be deleted.");
        if (configEntries.EnforceTwoFactorRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
            return TypedResults.Conflict($"The role '{role.Name}' is listed in OpenIddict:EnforceTwoFactorRoles.");

        // Deleting a required role would lock everyone out of those applications.
        var requiredBy = new List<string>();
        await foreach (var app in applicationManager.ListAsync())
        {
            var required = await ApplicationDescriptorBuilder.GetRequiredRolesAsync(applicationManager, app);
            if (required.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
                requiredBy.Add(await applicationManager.GetDisplayNameAsync(app) ?? await applicationManager.GetClientIdAsync(app) ?? "");
        }
        if (requiredBy.Count > 0)
            return TypedResults.Conflict($"The role '{role.Name}' is required by {string.Join(", ", requiredBy)}. Remove it from those applications first.");

        var result = await roleManager.DeleteAsync(role);
        audit.Log(SecurityEvent.RoleDeleted, success: result.Succeeded, detail: $"role '{role.Name}'");
        if (!result.Succeeded)
            return TypedResults.Conflict(string.Join(" ", result.Errors.Select(e => e.Description)));

        return TypedResults.Ok();
    }

    private async Task<RoleInfo> ToInfoAsync(IdentityRole role) => new()
    {
        Id = role.Id,
        Name = role.Name ?? "",
        UserCount = await dbContext.UserRoles.CountAsync(ur => ur.RoleId == role.Id),
        IsConfigManaged = configEntries.IsConfigRole(role.Name),
    };

    private const string InvalidNameMessage =
        "A role name must be 1-64 characters: letters, digits, spaces, '.', '_' or '-'.";
}
