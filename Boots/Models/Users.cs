namespace Boots.Models;

public class Users
{
    public int UserId { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string Login { get; set; } = string.Empty;
    public int RoleId { get; set; }
}
