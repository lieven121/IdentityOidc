namespace Identity.App.EndPoints.Admin.Models;

public class SetClientSecretDto
{
    /// <summary>
    /// A custom secret of at least 24 characters, or empty to generate one.
    /// </summary>
    public string? ClientSecret { get; set; }
}
