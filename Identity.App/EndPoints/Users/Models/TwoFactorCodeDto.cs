namespace Identity.App.EndPoints.Users.Models;

public class TwoFactorCodeDto
{
    /// <summary>
    /// A current authenticator code or an unused recovery code.
    /// </summary>
    public required string Code { get; set; }
}
