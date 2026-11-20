using Boots.Models;
using Microsoft.EntityFrameworkCore;

namespace Boots.Data;

public class AppDBContext : DbContext
{
    public DbSet<Products> Products => Set<Products>();
    public DbSet<Categories> Categories => Set<Categories>();
    public DbSet<Subcategories> Subcategories => Set<Subcategories>();
    public DbSet<Manufacturers> Manufacturers => Set<Manufacturers>();
    public DbSet<Users> Users => Set<Users>();
    public DbSet<StockItems> StockItems => Set<StockItems>();
    public DbSet<Orders> Orders => Set<Orders>();
    public DbSet<OrderItems> OrderItems => Set<OrderItems>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
             "Server=10.211.55.2,1433;Database=ShopBootsDB;" +
             "User Id=sa;Password=StrongPass123;TrustServerCertificate=True;");

    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categories>()
            .ToTable("Categories", "dbo")
            .HasKey(category => category.CategoryId);

        modelBuilder.Entity<Subcategories>()
            .ToTable("Subcategories", "dbo")
            .HasKey(subcategory => subcategory.SubcategoryId);

        modelBuilder.Entity<Manufacturers>()
            .ToTable("Manufacturers", "dbo")
            .HasKey(manufacturer => manufacturer.ManufacturerId);

        var Product = modelBuilder.Entity<Products>();
        modelBuilder.Entity<Users>().HasKey(user => user.UserId);

        Product.ToTable("Products", "dbo");
        Product.HasKey(item => item.ProductId);

        Product.Property(item => item.ProductName).HasMaxLength(255);
        Product.Property(item => item.ImageFile).HasMaxLength(255);
        Product.Property(item => item.Price).HasPrecision(18, 2);

        var user = modelBuilder.Entity<Users>();

        user.ToTable("Users", "dbo");
        user.Property(item => item.UserId);
        user.Property(item => item.Login).HasMaxLength(100);

        modelBuilder.Entity<StockItems>().HasKey(stock => stock.StockItemId);

        modelBuilder.Entity<Orders>()
            .ToTable("Orders", "dbo")
            .HasKey(order => order.OrderId);

        var orderItem = modelBuilder.Entity<OrderItems>();
        orderItem.ToTable("OrderItems", "dbo");
        orderItem.HasKey(item => item.OrderItemId);
        orderItem.Property(item => item.UnitPrice).HasPrecision(18, 2);

    }
}


