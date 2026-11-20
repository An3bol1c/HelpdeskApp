namespace HelpdeskApp.Models;

public class UserRolesViewModel
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public IList<string> Roles { get; set; } = [];
    public IList<string> AvailableRoles { get; set; } = [];
}
