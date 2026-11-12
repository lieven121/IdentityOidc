namespace Identity.App.EndPoints.Users.Models;

public class TwoFactorSetupDto
{
    public required string SharedKey { get; set; }
    public required string AuthenticatorUri { get; set; }
}
