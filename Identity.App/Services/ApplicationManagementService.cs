using Identity.App.EndPoints.Admin.Models;
using Identity.App.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.App.Services;

/// <summary>
/// Admin management of OpenIddict applications. Applications declared in configuration are
/// recreated on every start, so they are read-only here.
/// </summary>
[RegisterScoped]
public partial class ApplicationManagementService(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    RoleManager<IdentityRole> roleManager,
    ConfigEntries configEntries,
    SecurityAudit audit)
{
    public const int MinimumSecretLength = 24;

    private const string ConfigManagedMessage =
        "This application is managed by configuration and can't be changed here.";

    [GeneratedRegex("^[A-Za-z0-9._-]{3,100}$")]
    private static partial Regex ClientIdPattern();

    [GeneratedRegex("^[a-z0-9:_.-]{1,100}$")]
    private static partial Regex ScopePattern();

    public async Task<List<ApplicationDto>> ListAsync()
    {
        var result = new List<ApplicationDto>();
        await foreach (var app in applicationManager.ListAsync())
            result.Add(await ToDtoAsync(app));

        return result.OrderBy(a => a.DisplayName ?? a.ClientId, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<ApplicationDto?> GetAsync(string clientId)
    {
        var app = await applicationManager.FindByClientIdAsync(clientId);
        return app == null ? null : await ToDtoAsync(app);
    }

    /// <summary>
    /// Scopes an application can be given: those in the scope store plus the admin scope.
    /// </summary>
    public async Task<List<string>> ListScopesAsync()
    {
        var scopes = new SortedSet<string>(StringComparer.Ordinal) { Models.Consts.Scopes.Admin };
        await foreach (var scope in scopeManager.ListAsync())
        {
            var name = await scopeManager.GetNameAsync(scope);
            if (!string.IsNullOrEmpty(name))
                scopes.Add(name);
        }
        return scopes.ToList();
    }

    public async Task<Results<Ok<ApplicationSecretDto>, BadRequest<string>, Conflict<string>>> CreateAsync(SaveApplicationDto dto)
    {
        var clientId = dto.ClientId?.Trim() ?? "";
        if (!ClientIdPattern().IsMatch(clientId))
            return TypedResults.BadRequest("The client id must be 3-100 characters: letters, digits, '.', '_' or '-'.");
        if (configEntries.IsConfigApplication(clientId) || await applicationManager.FindByClientIdAsync(clientId) != null)
            return TypedResults.Conflict($"An application with client id '{clientId}' already exists.");

        var (config, error) = await ValidateAsync(dto, clientId);
        if (config == null)
            return TypedResults.BadRequest(error!);

        var confidential = IsConfidential(dto.ClientType);
        string? secret = confidential ? GenerateSecret() : null;
        config.ClientSecret = secret!;

        try
        {
            await applicationManager.CreateAsync(ApplicationDescriptorBuilder.Build(config));
        }
        catch (OpenIddictExceptions.ValidationException ex)
        {
            return TypedResults.BadRequest(string.Join(" ", ex.Results.Select(r => r.ErrorMessage)));
        }

        foreach (var scope in ApplicationDescriptorBuilder.GetScopes(config))
            await ApplicationDescriptorBuilder.AddScopeResourceAsync(scopeManager, scope, clientId);

        audit.Log(SecurityEvent.ApplicationCreated, clientId: clientId, detail: Describe(config, confidential));
        return TypedResults.Ok(new ApplicationSecretDto { ClientId = clientId, ClientSecret = secret });
    }

    public async Task<Results<Ok<ApplicationSecretDto>, BadRequest<string>, Conflict<string>, NotFound>> UpdateAsync(
        string clientId, SaveApplicationDto dto)
    {
        var app = await applicationManager.FindByClientIdAsync(clientId);
        if (app == null)
            return TypedResults.NotFound();
        if (configEntries.IsConfigApplication(clientId))
            return TypedResults.Conflict(ConfigManagedMessage);

        clientId = (await applicationManager.GetClientIdAsync(app))!;
        var (config, error) = await ValidateAsync(dto, clientId);
        if (config == null)
            return TypedResults.BadRequest(error!);

        var existing = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(existing, app);

        var confidential = IsConfidential(dto.ClientType);
        var descriptor = ApplicationDescriptorBuilder.Build(config, confidential);

        // Keep the stored (hashed) secret unless the client becomes confidential, which needs a new one.
        string? newSecret = null;
        if (confidential)
        {
            if (existing.ClientType == ClientTypes.Confidential && !string.IsNullOrEmpty(existing.ClientSecret))
                descriptor.ClientSecret = existing.ClientSecret;
            else
                descriptor.ClientSecret = newSecret = GenerateSecret();
        }

        try
        {
            await applicationManager.UpdateAsync(app, descriptor);
        }
        catch (OpenIddictExceptions.ValidationException ex)
        {
            return TypedResults.BadRequest(string.Join(" ", ex.Results.Select(r => r.ErrorMessage)));
        }

        var oldScopes = ApplicationDescriptorBuilder.GetScopes(existing);
        var newScopes = ApplicationDescriptorBuilder.GetScopes(descriptor);
        foreach (var scope in oldScopes.Except(newScopes))
            await ApplicationDescriptorBuilder.RemoveScopeResourceAsync(scopeManager, scope, clientId);
        foreach (var scope in newScopes.Except(oldScopes))
            await ApplicationDescriptorBuilder.AddScopeResourceAsync(scopeManager, scope, clientId);

        audit.Log(SecurityEvent.ApplicationUpdated, clientId: clientId, detail: Describe(config, confidential));
        if (newSecret != null)
            audit.Log(SecurityEvent.ApplicationSecretChanged, clientId: clientId, detail: "generated (switched to confidential)");

        return TypedResults.Ok(new ApplicationSecretDto { ClientId = clientId, ClientSecret = newSecret });
    }

    public async Task<Results<Ok<ApplicationSecretDto>, BadRequest<string>, Conflict<string>, NotFound>> SetSecretAsync(
        string clientId, SetClientSecretDto dto)
    {
        var app = await applicationManager.FindByClientIdAsync(clientId);
        if (app == null)
            return TypedResults.NotFound();
        if (configEntries.IsConfigApplication(clientId))
            return TypedResults.Conflict("This application's secret is managed by configuration and can't be changed here.");
        if (!await applicationManager.HasClientTypeAsync(app, ClientTypes.Confidential))
            return TypedResults.BadRequest("Public applications don't have a secret.");

        var custom = !string.IsNullOrWhiteSpace(dto.ClientSecret);
        if (custom && dto.ClientSecret!.Length < MinimumSecretLength)
            return TypedResults.BadRequest($"The secret must be at least {MinimumSecretLength} characters.");

        var secret = custom ? dto.ClientSecret! : GenerateSecret();

        // Only the new hash is stored: the previous secret stops working immediately.
        await applicationManager.UpdateAsync(app, secret);

        clientId = (await applicationManager.GetClientIdAsync(app))!;
        audit.Log(SecurityEvent.ApplicationSecretChanged, clientId: clientId, detail: custom ? "custom" : "generated");
        return TypedResults.Ok(new ApplicationSecretDto { ClientId = clientId, ClientSecret = secret });
    }

    public async Task<Results<Ok, Conflict<string>, NotFound>> DeleteAsync(string clientId)
    {
        var app = await applicationManager.FindByClientIdAsync(clientId);
        if (app == null)
            return TypedResults.NotFound();
        if (configEntries.IsConfigApplication(clientId))
            return TypedResults.Conflict(ConfigManagedMessage);

        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, app);

        // The EF Core store also deletes the application's authorizations and tokens.
        await applicationManager.DeleteAsync(app);

        foreach (var scope in ApplicationDescriptorBuilder.GetScopes(descriptor))
            await ApplicationDescriptorBuilder.RemoveScopeResourceAsync(scopeManager, scope, descriptor.ClientId!);

        audit.Log(SecurityEvent.ApplicationDeleted, clientId: descriptor.ClientId);
        return TypedResults.Ok();
    }

    public async Task<Results<Ok, BadRequest<string>, Conflict<string>, NotFound>> UpdateRequiredRolesAsync(
        string clientId, List<string>? requiredRoles)
    {
        var app = await applicationManager.FindByClientIdAsync(clientId);
        if (app == null)
            return TypedResults.NotFound();
        if (configEntries.IsConfigApplication(clientId))
            return TypedResults.Conflict(ConfigManagedMessage);

        var (roles, error) = await NormalizeRolesAsync(requiredRoles);
        if (roles == null)
            return TypedResults.BadRequest(error!);

        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, app);
        descriptor.Properties[ApplicationDescriptorBuilder.RequiredRolesProperty] =
            System.Text.Json.JsonSerializer.SerializeToElement(roles);
        await applicationManager.UpdateAsync(app, descriptor);

        audit.Log(SecurityEvent.ApplicationRolesUpdated, clientId: clientId,
            detail: $"required roles '{string.Join(", ", roles)}'");
        return TypedResults.Ok();
    }

    private async Task<ApplicationDto> ToDtoAsync(object app)
    {
        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, app);

        return new ApplicationDto
        {
            ClientId = descriptor.ClientId ?? "",
            DisplayName = descriptor.DisplayName,
            ClientType = descriptor.ClientType ?? ClientTypes.Public,
            Pkce = descriptor.Requirements.Contains(Requirements.Features.ProofKeyForCodeExchange),
            RedirectUris = descriptor.RedirectUris.Select(u => u.OriginalString).ToList(),
            PostLogoutRedirectUris = descriptor.PostLogoutRedirectUris.Select(u => u.OriginalString).ToList(),
            Scopes = ApplicationDescriptorBuilder.GetScopes(descriptor),
            RequiredRoles = ApplicationDescriptorBuilder.GetRequiredRoles(descriptor),
            IsConfigManaged = configEntries.IsConfigApplication(descriptor.ClientId),
        };
    }

    // Returns the application as config (without secret), or an error message.
    private async Task<(ApplicationConfig? Config, string? Error)> ValidateAsync(SaveApplicationDto dto, string clientId)
    {
        var displayName = dto.DisplayName?.Trim();
        if (string.IsNullOrEmpty(displayName) || displayName.Length > 100)
            return (null, "A display name of at most 100 characters is required.");

        if (dto.ClientType != ClientTypes.Public && dto.ClientType != ClientTypes.Confidential)
            return (null, "The client type must be 'public' or 'confidential'.");

        var redirectUris = NormalizeUris(dto.RedirectUris, out var invalid);
        var postLogoutUris = NormalizeUris(dto.PostLogoutRedirectUris, out var invalidPostLogout);
        var invalidUri = invalid ?? invalidPostLogout;
        if (invalidUri != null)
            return (null, $"'{invalidUri}' is not a valid absolute URI without a fragment.");

        var scopes = (dto.Scopes ?? [])
            .Select(s => s.Trim())
            .Where(s => s.Length > 0 && !IsDefaultScope(s))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var invalidScope = scopes.FirstOrDefault(s => !ScopePattern().IsMatch(s));
        if (invalidScope != null)
            return (null, $"'{invalidScope}' is not a valid scope: use lowercase letters, digits, ':', '_', '.' or '-'.");

        var (roles, rolesError) = await NormalizeRolesAsync(dto.RequiredRoles);
        if (roles == null)
            return (null, rolesError);

        return (new ApplicationConfig
        {
            Name = displayName,
            ClientId = clientId,
            Scope = string.Join(' ', scopes),
            RedirectUri = redirectUris,
            PostLogoutRedirectUri = postLogoutUris,
            PKCE = dto.Pkce,
            RequiredRoles = roles,
        }, null);
    }

    // Resolves role names to their stored casing; fails on unknown roles.
    private async Task<(List<string>? Roles, string? Error)> NormalizeRolesAsync(IEnumerable<string>? requested)
    {
        var roles = new List<string>();
        foreach (var name in (requested ?? []).Select(r => r.Trim()).Where(r => r.Length > 0))
        {
            var role = await roleManager.FindByNameAsync(name);
            if (role?.Name == null)
                return (null, $"The role '{name}' does not exist.");
            if (!roles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
                roles.Add(role.Name);
        }
        return (roles, null);
    }

    private static List<string> NormalizeUris(IEnumerable<string>? uris, out string? invalid)
    {
        invalid = null;
        var result = new List<string>();
        foreach (var value in (uris ?? []).Select(u => u.Trim()).Where(u => u.Length > 0))
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.Fragment))
            {
                invalid = value;
                return [];
            }
            if (!result.Contains(value, StringComparer.Ordinal))
                result.Add(value);
        }
        return result;
    }

    private static bool IsDefaultScope(string scope) =>
        scope is Scopes.OpenId or Scopes.OfflineAccess or Scopes.Email or Scopes.Profile or Scopes.Roles;

    private static bool IsConfidential(string clientType) => clientType == ClientTypes.Confidential;

    private static string GenerateSecret() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    private static string Describe(ApplicationConfig config, bool confidential) =>
        $"{(confidential ? "confidential" : "public")}, scopes '{config.Scope}', required roles '{string.Join(", ", config.RequiredRoles)}', " +
        $"{config.RedirectUri.Count()} redirect uri(s)";
}
