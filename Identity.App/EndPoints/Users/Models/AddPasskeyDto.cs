namespace Identity.App.EndPoints.Users.Models;

public class AddPasskeyDto
{
    /// <summary>
    /// The JSON-serialized result of navigator.credentials.create().
    /// </summary>
    public required string CredentialJson { get; set; }
    public string? Name { get; set; }
}
