namespace Identity.App.EndPoints.Admin.Models;

public class RoleMemberDto
{
    public required string Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }

    /// <summary>
    /// Declared in OpenIddict:Users; role membership is re-synced on every start and read-only here.
    /// </summary>
    public bool IsConfigManaged { get; set; }
}
