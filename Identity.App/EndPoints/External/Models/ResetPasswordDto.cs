namespace Identity.App.EndPoints.External.Models;

public class ResetPasswordDto
{
    public bool ResetSilent { get; set; } = false;

    /// <summary>
    /// If NewPasword is null the user will get a reset password email.
    /// </summary>
    public string? NewPassword { get; set; }
}
