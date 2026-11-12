using Identity.App.Data;
using Microsoft.AspNetCore.Identity;

namespace Identity.App.Services;

[RegisterScoped]
public class UserRoleService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserRoleService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<bool> EnsureRoleExistsAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            return result.Succeeded;
        }
        return true;
    }

    public async Task<bool> EnsureUserRoleAsync(ApplicationUser user, bool active, params string[] roleName)
    {
        foreach (var role in roleName)
        {
            var res = active
                ? await AddUserToRoleAsync(user, role)
                : await RemoveUserFromRoleAsync(user, role);
            if (!res) return false;
        }

        return true;
    }


    public async Task<bool> AddUserToRoleAsync(ApplicationUser user, string roleName)
    {
        await EnsureRoleExistsAsync(roleName);
        
        if (!await _userManager.IsInRoleAsync(user, roleName))
        {
            var result = await _userManager.AddToRoleAsync(user, roleName);
            return result.Succeeded;
        }
        return true;
    }

    public async Task<bool> RemoveUserFromRoleAsync(ApplicationUser user, string roleName)
    {
        if (await _userManager.IsInRoleAsync(user, roleName))
        {
            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return result.Succeeded;
        }
        return true;
    }

    public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
    {
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<bool> IsUserInRoleAsync(ApplicationUser user, string roleName)
    {
        return await _userManager.IsInRoleAsync(user, roleName);
    }
}
