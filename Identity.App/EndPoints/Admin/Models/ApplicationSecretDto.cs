namespace Identity.App.EndPoints.Admin.Models;

public class ApplicationSecretDto
{
    public required string ClientId { get; set; }

    /// <summary>
    /// Set only when a new secret was issued. It is stored hashed and can't be retrieved again.
    /// </summary>
    public string? ClientSecret { get; set; }
}
