namespace Boots.Models;

public class Orders
{
    public int OrderId { get; set; }
    public DateOnly OrderDate { get; set; }
    public int UserId { get; set; }
}
