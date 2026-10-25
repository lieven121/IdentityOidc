namespace Identity.App.EndPoints.Admin.Models;

public class SaveApplicationDto
{
    /// <summary>
    /// Only used when creating; the client id can't be changed afterwards.
    /// </summary>
    public string? ClientId { get; set; }
    public required string DisplayName { get; set; }

    /// <summary>
    /// "public" or "confidential".
    /// </summary>
    public required string ClientType { get; set; }

    /// <summary>
    /// Always on for public clients.
    /// </summary>
    public bool Pkce { get; set; }
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public List<string> Scopes { get; set; } = [];
    public List<string> RequiredRoles { get; set; } = [];
}
