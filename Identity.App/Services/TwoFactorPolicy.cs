using Identity.App.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Identity.App.Services;

/// <summary>
/// Decides whether a user must use multi-factor authentication and whether they already do.
/// Required: OpenIddict:EnforceTwoFactor, or membership of a role in OpenIddict:EnforceTwoFactorRoles.
/// Satisfied: TOTP enabled, or at least one registered passkey.
/// </summary>
[RegisterScoped]
public class TwoFactorPolicy(UserManager<ApplicationUser> userManager, IConfiguration configuration)
{
    public async Task<bool> IsRequiredAsync(ApplicationUser user)
    {
        if (configuration.GetValue<bool>("OpenIddict:EnforceTwoFactor"))
            return true;

        var enforcedRoles = configuration.GetSection("OpenIddict:EnforceTwoFactorRoles").Get<string[]>();
        if (enforcedRoles is not { Length: > 0 })
            return false;

        var roles = await userManager.GetRolesAsync(user);
        return roles.Any(role => enforcedRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    public async Task<bool> IsSatisfiedAsync(ApplicationUser user)
    {
        if (user.TwoFactorEnabled)
            return true;

        return (await userManager.GetPasskeysAsync(user)).Count > 0;
    }

    public async Task<bool> NeedsSetupAsync(ApplicationUser user)
        => await IsRequiredAsync(user) && !await IsSatisfiedAsync(user);
}

public static class TwoFactorPolicyExtensions
{
    /// <summary>
    /// Fails authorization while the signed-in user still has to set up 2FA.
    /// </summary>
    public static AuthorizationPolicyBuilder RequireTwoFactorCompliance(this AuthorizationPolicyBuilder policy)
        => policy.RequireAssertion(async context =>
        {
            if (context.Resource is not HttpContext httpContext)
                return false;

            var services = httpContext.RequestServices;
            var user = await services.GetRequiredService<UserManager<ApplicationUser>>().GetUserAsync(context.User);
            return user != null && !await services.GetRequiredService<TwoFactorPolicy>().NeedsSetupAsync(user);
        });
}
