namespace Identity.App.EndPoints.Users.Models;

public class RecoveryCodesDto
{
    public required IEnumerable<string> RecoveryCodes { get; set; }
}
