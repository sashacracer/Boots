using System.Windows;
using Boots.Data;
using Boots.helper;
using Boots.Models;

namespace Boots;

public partial class ProductDetailsWindow : Window
{
    private readonly Helper _messages = new();
    private int _productId;
    private string _productName = string.Empty;
    private decimal _unitPrice;

    public ProductDetailsWindow()
    {
        InitializeComponent();
    }

    internal OrderDraftLine? SelectedOrderLine { get; private set; }

    internal bool LoadProduct(int productId, bool selectForOrder = false)
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

        _productId = productId;
        _productName = product.ProductName;
        _unitPrice = ProductPricing.GetPrice(product.Price, product.HasDiscount);

        DataContext = new
        {
            product.ProductName,
            product.ManufacturerName,
            Category = $"{product.CategoryName} / {product.SubcategoryName}",
            Composition = string.IsNullOrWhiteSpace(product.Composition) ? "Состав не указан." : product.Composition,
            Description = string.IsNullOrWhiteSpace(product.Description) ? "Описание не указано." : product.Description,
            ImagePath = ProductImages.GetSource(product.ImageFile),
            Price = _unitPrice
        };

        var sizes = (
            from stock in db.StockItems
            join size in db.Sizes on stock.SizeId equals size.SizeId
            where stock.ProductId == productId
            group stock by new { size.SizeId, size.SizeValue } into sizeStock
            orderby sizeStock.Key.SizeValue
            select new ProductSize(
                sizeStock.Key.SizeId,
                sizeStock.Key.SizeValue,
                sizeStock.Sum(item => item.AvailableQuantity))).ToList();

        SizeGrid.ItemsSource = sizes;

        if (selectForOrder && sizes.Any(size => size.AvailableQuantity > 0))
        {
            OrderSelection.Visibility = Visibility.Visible;
        }

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

    private void AddToOrder_Click(object sender, RoutedEventArgs e)
    {
        if (SizeGrid.SelectedItem is not ProductSize size)
        {
            _messages.ShowWarning("Выбери размер в таблице.");
            return;
        }

        if (!int.TryParse(QuantityBox.Text, out var quantity) || quantity <= 0)
        {
            _messages.ShowWarning("Введи целое количество пар больше нуля.");
            return;
        }

        if (quantity > size.AvailableQuantity)
        {
            _messages.ShowWarning($"Для выбранного размера доступно {size.AvailableQuantity} пар.");
            return;
        }

        SelectedOrderLine = new OrderDraftLine(_productId, _productName, size.SizeId,
            size.SizeValue, quantity, size.AvailableQuantity, _unitPrice);
        DialogResult = true;
    }

    private sealed record ProductSize(int SizeId, int SizeValue, int AvailableQuantity);
}
