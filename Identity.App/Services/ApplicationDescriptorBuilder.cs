using Identity.App.Models;
using OpenIddict.Abstractions;
using System.Text.Json;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.App.Services;

/// <summary>
/// Builds OpenIddict application descriptors, shared by the startup worker (config applications)
/// and the admin API (applications managed in the UI) so both get the same permission set.
/// </summary>
public static class ApplicationDescriptorBuilder
{
    public const string RequiredRolesProperty = "RequiredRoles";

    // Granted to every application; not listed as selectable scopes.
    private static readonly string[] DefaultScopePermissions =
    [
        Permissions.Scopes.Email,
        Permissions.Scopes.Profile,
        Permissions.Scopes.Roles,
        Permissions.Prefixes.Scope + Scopes.OpenId,
        Permissions.Prefixes.Scope + Scopes.OfflineAccess,
    ];

    /// <param name="confidential">Overrides the client type, which otherwise follows whether a secret is set.</param>
    public static OpenIddictApplicationDescriptor Build(ApplicationConfig config, bool? confidential = null)
    {
        var app = new OpenIddictApplicationDescriptor
        {
            ClientId = config.ClientId,
            DisplayName = config.Name,
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.EndSession,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
            },
            ClientType = (confidential ?? !string.IsNullOrWhiteSpace(config.ClientSecret)) ? ClientTypes.Confidential : ClientTypes.Public,
            ClientSecret = string.IsNullOrWhiteSpace(config.ClientSecret) ? null : config.ClientSecret,
        };

        foreach (var permission in DefaultScopePermissions)
            app.Permissions.Add(permission);

        if (config.RequiredRoles != null && config.RequiredRoles.Any())
            app.Properties[RequiredRolesProperty] = JsonSerializer.SerializeToElement(config.RequiredRoles.ToList());

        foreach (var scope in GetScopes(config))
            app.Permissions.Add(Permissions.Prefixes.Scope + scope);

        if (app.ClientType == ClientTypes.Confidential)
        {
            app.Permissions.Add(Permissions.GrantTypes.ClientCredentials);
            app.Permissions.Add(Permissions.ResponseTypes.Token);
        }

        // Public clients can't authenticate at the token endpoint, so PKCE is their only
        // protection against authorization code interception.
        if (config.PKCE || app.ClientType == ClientTypes.Public)
            app.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);

        foreach (var uri in config.RedirectUri ?? [])
            app.RedirectUris.Add(new Uri(uri));

        foreach (var uri in config.PostLogoutRedirectUri ?? [])
            app.PostLogoutRedirectUris.Add(new Uri(uri));

        return app;
    }

    public static IEnumerable<string> GetScopes(ApplicationConfig config) =>
        (config.Scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct();

    /// <summary>The selectable (non-default) scopes an application is permitted.</summary>
    public static List<string> GetScopes(OpenIddictApplicationDescriptor descriptor) =>
        descriptor.Permissions
            .Where(p => p.StartsWith(Permissions.Prefixes.Scope, StringComparison.Ordinal) && !DefaultScopePermissions.Contains(p))
            .Select(p => p[Permissions.Prefixes.Scope.Length..])
            .ToList();

    public static List<string> GetRequiredRoles(OpenIddictApplicationDescriptor descriptor) =>
        descriptor.Properties.TryGetValue(RequiredRolesProperty, out var roles) ? ParseRoles(roles) : [];

    public static async Task<List<string>> GetRequiredRolesAsync(IOpenIddictApplicationManager manager, object application)
    {
        var properties = await manager.GetPropertiesAsync(application);
        return properties.TryGetValue(RequiredRolesProperty, out var roles) ? ParseRoles(roles) : [];
    }

    private static List<string> ParseRoles(JsonElement roles) =>
        roles.ValueKind != JsonValueKind.Array
            ? []
            : roles.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString() ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

    /// <summary>
    /// Links a scope to a client id as a resource (token audience), creating the scope if needed.
    /// Mirrors what the startup worker does for config applications.
    /// </summary>
    public static async Task AddScopeResourceAsync(IOpenIddictScopeManager scopeManager, string scope, string clientId)
    {
        var entity = await scopeManager.FindByNameAsync(scope);
        if (entity == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = scope,
                DisplayName = scope,
                Resources = { clientId }
            });
            return;
        }

        var descriptor = new OpenIddictScopeDescriptor();
        await scopeManager.PopulateAsync(descriptor, entity);
        if (descriptor.Resources.Add(clientId))
            await scopeManager.UpdateAsync(entity, descriptor);
    }

    public static async Task RemoveScopeResourceAsync(IOpenIddictScopeManager scopeManager, string scope, string clientId)
    {
        var entity = await scopeManager.FindByNameAsync(scope);
        if (entity == null)
            return;

        var descriptor = new OpenIddictScopeDescriptor();
        await scopeManager.PopulateAsync(descriptor, entity);
        if (descriptor.Resources.Remove(clientId))
            await scopeManager.UpdateAsync(entity, descriptor);
    }
}
