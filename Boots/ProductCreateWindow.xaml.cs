using Boots.Data;
using Boots.helper;
using Boots.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Boots;

public partial class ProductCreateWindow : Window
{
    private readonly Helper _messages = new();
    private readonly List<StockRow> _stock;
    private string? _photoPath;

    public ProductCreateWindow()
    {
        InitializeComponent();

        using var db = new AppDBContext();

        var subcategories = (
            from subcategory in db.Subcategories
            join category in db.Categories on subcategory.CategoryId equals category.CategoryId
            orderby category.CategoryName, subcategory.SubcategoryName
            select new
            {
                subcategory.SubcategoryId,
                Name = category.CategoryName + " / " + subcategory.SubcategoryName
            }).ToList();

        SubcategoryBox.ItemsSource = subcategories;
        ManufacturerBox.ItemsSource = db.Manufacturers.OrderBy(item => item.ManufacturerName).ToList();

        _stock = db.Sizes
            .OrderBy(size => size.SizeValue)
            .Select(size => new StockRow { SizeId = size.SizeId, SizeValue = size.SizeValue })
            .ToList();

        StockGrid.ItemsSource = _stock;
    }

    private void ChoosePhoto_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выбрать фотографию товара",
            Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            PhotoPreview.Source = ProductImages.LoadPreview(dialog.FileName);
            _photoPath = dialog.FileName;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or NotSupportedException or FormatException)
        {
            _messages.ShowWarning("Не удалось прочитать изображение. Выбери исправный файл PNG, JPEG или BMP.");
        }
    }

    private void SaveProduct_Click(object sender, RoutedEventArgs e)
    {
        var name = ProductNameBox.Text.Trim();
        if (name.Length == 0 || SubcategoryBox.SelectedValue is not int subcategoryId ||
            ManufacturerBox.SelectedValue is not int manufacturerId)
        {
            _messages.ShowWarning("Введи название и выбери подкатегорию и производителя.");
            return;
        }

        var priceText = PriceBox.Text.Trim().Replace(',', '.');
        if (!decimal.TryParse(priceText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var price) ||
            price <= 0 || price > 9999999999999999.99m || price != decimal.Round(price, 2))
        {
            _messages.ShowWarning("Введи цену больше нуля, максимум с двумя знаками после запятой.");
            return;
        }

        if (!StockGrid.CommitEdit(DataGridEditingUnit.Cell, true) ||
            !StockGrid.CommitEdit(DataGridEditingUnit.Row, true) ||
            _stock.Any(size => size.Quantity < 0))
        {
            _messages.ShowWarning("Остаток для каждого размера должен быть целым числом от нуля.");
            return;
        }

        string? imageFile = null;
        try
        {
            if (_photoPath is not null)
            {
                imageFile = ProductImages.CopyImported(_photoPath);
            }

            var product = new Products
            {
                ProductName = name,
                SubcategoryId = subcategoryId,
                ManufacturerId = manufacturerId,
                Description = DescriptionBox.Text.Trim(),
                Composition = CompositionBox.Text.Trim(),
                ImageFile = imageFile,
                Price = price
            };
            var quantities = _stock.Where(size => size.Quantity > 0)
                .ToDictionary(size => size.SizeId, size => size.Quantity);

            using var db = new AppDBContext();
            ProductCreation.Save(db, product, quantities);
            DialogResult = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _messages.ShowErr("Не удалось сохранить фото. Проверь, доступен ли исходный файл, или выбери его заново.");
        }
        catch (DbUpdateException)
        {
            ProductImages.RemoveImported(imageFile);
            _messages.ShowErr("Не удалось сохранить товар. Проверь выбранную подкатегорию и производителя.");
        }
        catch (SqlException)
        {
            _messages.ShowErr("Связь с базой прервалась при сохранении. Проверь каталог перед повторной попыткой.");
        }
    }

    private sealed class StockRow
    {
        public int SizeId { get; init; }
        public int SizeValue { get; init; }
        public int Quantity { get; set; }
    }
}
