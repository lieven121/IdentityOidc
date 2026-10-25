namespace Identity.App.EndPoints.Admin.Models;

public class ApplicationDto
{
    public required string ClientId { get; set; }
    public string? DisplayName { get; set; }

    /// <summary>
    /// "public" or "confidential".
    /// </summary>
    public required string ClientType { get; set; }
    public bool Pkce { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];

    /// <summary>
    /// Scopes beyond the defaults (openid, offline_access, email, profile, roles) every application gets.
    /// </summary>
    public List<string> Scopes { get; set; } = [];
    public List<string> RequiredRoles { get; set; } = [];

    /// <summary>
    /// Declared in OpenIddict:ApplicationConfigs; recreated on every start and read-only here.
    /// </summary>
    public bool IsConfigManaged { get; set; }
}
