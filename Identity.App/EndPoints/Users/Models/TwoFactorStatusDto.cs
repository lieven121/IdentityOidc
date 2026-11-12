namespace Identity.App.EndPoints.Users.Models;

public class TwoFactorStatusDto
{
    public bool Is2faEnabled { get; set; }
    public bool HasAuthenticator { get; set; }
    public int RecoveryCodesLeft { get; set; }
}
