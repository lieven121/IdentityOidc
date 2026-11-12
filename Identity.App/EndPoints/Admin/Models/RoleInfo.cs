namespace Identity.App.EndPoints.Admin.Models;

public class RoleInfo
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public int UserCount { get; set; }

    /// <summary>
    /// Declared in OpenIddict:Roles (or Admin); can't be renamed or deleted.
    /// </summary>
    public bool IsConfigManaged { get; set; }
}
