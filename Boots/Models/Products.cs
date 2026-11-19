namespace Boots.Models;

public class Products
{
    public int ProductId { get; set; }
    public int SubcategoryId { get; set; }
    public int ManufacturerId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageFile { get; set; }
    public string? Description { get; set; }
    public string? Composition { get; set; }
    public decimal Price { get; set; }
}
