using System.Windows;
using Boots.Data;

namespace Boots;

public partial class ProductDetailsWindow : Window
{
    public ProductDetailsWindow()
    {
        InitializeComponent();
    }

    internal bool LoadProduct(int productId)
    {
        using var db = new AppDBContext();

        var orderedProductIds = ProductPricing.GetOrderedProductIds(
            db, DateOnly.FromDateTime(DateTime.Today));

        var product = (
            from item in db.Products
            join subcategory in db.Subcategories on item.SubcategoryId equals subcategory.SubcategoryId
            join category in db.Categories on subcategory.CategoryId equals category.CategoryId
            join manufacturer in db.Manufacturers on item.ManufacturerId equals manufacturer.ManufacturerId
            where item.ProductId == productId
            select new
            {
                item.ProductName,
                item.ImageFile,
                item.Description,
                item.Composition,
                item.Price,
                HasDiscount = !orderedProductIds.Contains(item.ProductId),
                category.CategoryName,
                subcategory.SubcategoryName,
                manufacturer.ManufacturerName
            }).SingleOrDefault();

        if (product is null)
        {
            return false;
        }

        DataContext = new
        {
            product.ProductName,
            product.ManufacturerName,
            Category = $"{product.CategoryName} / {product.SubcategoryName}",
            Composition = string.IsNullOrWhiteSpace(product.Composition) ? "Состав не указан." : product.Composition,
            Description = string.IsNullOrWhiteSpace(product.Description) ? "Описание не указано." : product.Description,
            ImagePath = $"/Boots;component/res/{product.ImageFile ?? "Чудо Обувь.png"}",
            Price = ProductPricing.GetPrice(product.Price, product.HasDiscount)
        };

        var sizes = (
            from stock in db.StockItems
            join size in db.Sizes on stock.SizeId equals size.SizeId
            where stock.ProductId == productId
            group stock by size.SizeValue into sizeStock
            orderby sizeStock.Key
            select new
            {
                SizeValue = sizeStock.Key,
                AvailableQuantity = sizeStock.Sum(item => item.AvailableQuantity)
            }).ToList();

        SizeGrid.ItemsSource = sizes;

        if (sizes.Count == 0)
        {
            SizeGrid.Visibility = Visibility.Collapsed;
            SizesStatus.Text = "Для этой модели размеры не указаны.";
            SizesStatus.Visibility = Visibility.Visible;
        }
        else if (sizes.All(size => size.AvailableQuantity == 0))
        {
            SizesStatus.Text = "Сейчас этой модели нет в наличии.";
            SizesStatus.Visibility = Visibility.Visible;
        }

        return true;
    }
}
