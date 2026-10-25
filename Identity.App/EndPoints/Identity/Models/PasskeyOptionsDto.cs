namespace Identity.App.EndPoints.Identity.Models;

public class PasskeyOptionsDto
{
    /// <summary>
    /// WebAuthn options as JSON, ready for PublicKeyCredential.parse*OptionsFromJSON or simplewebauthn.
    /// </summary>
    public required string OptionsJson { get; set; }
}
