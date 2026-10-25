using Identity.App.Models;
using Identity.App.Models.Consts;

namespace Identity.App.Services;

/// <summary>
/// Applications, roles and users declared in configuration. The startup worker recreates or
/// re-syncs these on every start, so they are read-only in the admin API.
/// </summary>
[RegisterSingleton]
public class ConfigEntries(IConfiguration configuration)
{
    public IReadOnlyList<ApplicationConfig> Applications =>
        configuration.GetSection("OpenIddict:ApplicationConfigs").Get<List<ApplicationConfig>>() ?? [];

    public IReadOnlyList<string> RoleNames =>
        configuration.GetSection("OpenIddict:Roles").Get<List<string>>() ?? [Roles.Admin, Roles.User];

    public IReadOnlyList<string> EnforceTwoFactorRoles =>
        configuration.GetSection("OpenIddict:EnforceTwoFactorRoles").Get<List<string>>() ?? [];

    public bool IsConfigApplication(string? clientId) =>
        !string.IsNullOrEmpty(clientId) &&
        Applications.Any(a => string.Equals(a.ClientId, clientId, StringComparison.OrdinalIgnoreCase));

    // Admin is always protected so admin access can't be removed from the UI.
    public bool IsConfigRole(string? name) =>
        !string.IsNullOrEmpty(name) &&
        (string.Equals(name, Roles.Admin, StringComparison.OrdinalIgnoreCase) ||
         RoleNames.Contains(name, StringComparer.OrdinalIgnoreCase));

    public bool IsConfigUser(string? email) =>
        !string.IsNullOrEmpty(email) &&
        (configuration.GetSection("OpenIddict:Users").Get<List<UserConfig>>() ?? [])
            .Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
}
