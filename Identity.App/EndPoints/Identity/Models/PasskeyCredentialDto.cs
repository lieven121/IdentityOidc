namespace Identity.App.EndPoints.Identity.Models;

public class PasskeyCredentialDto
{
    /// <summary>
    /// The JSON-serialized result of navigator.credentials.get() or navigator.credentials.create().
    /// </summary>
    public required string CredentialJson { get; set; }
}
