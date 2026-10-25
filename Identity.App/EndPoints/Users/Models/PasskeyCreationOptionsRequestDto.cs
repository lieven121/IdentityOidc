namespace Identity.App.EndPoints.Users.Models;

public class PasskeyCreationOptionsRequestDto
{
    /// <summary>
    /// Re-confirms the user before a new sign-in credential is added to the account.
    /// </summary>
    public required string CurrentPassword { get; set; }
}
