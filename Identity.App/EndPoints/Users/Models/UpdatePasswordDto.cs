﻿namespace Identity.App.EndPoints.Users.Models;

public class UpdatePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;

}
