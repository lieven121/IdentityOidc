using Microsoft.AspNetCore.Mvc;

namespace Identity.App.EndPoints.External.Models;

public class UsersFilter
{
    [FromQuery]
    public string Email { get; set; } = string.Empty;
    [FromQuery]
    public string Username { get; set; } = string.Empty;

    [FromQuery]
    public int Page { get; set; } = 0;
    [FromQuery]
    public int PageSize { get; set; } = 10;
    //public string SortBy { get; set; } = "Email";
    //public bool SortDescending { get; set; } = false;
}
