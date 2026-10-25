namespace Identity.App.EndPoints.Users.Models;

public class PasskeyDto
{
    /// <summary>
    /// The credential ID, base64url encoded.
    /// </summary>
    public required string Id { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsBackedUp { get; set; }
}
