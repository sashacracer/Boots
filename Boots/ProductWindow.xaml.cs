using Boots.Data;
using Boots.helper;
using Boots.Models;
using Boots.Static;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace Boots
{
    public partial class ProductWindow : Window
    {
        private readonly Helper _messages = new();
        private List<ProductCard> _products = [];

        public bool CanOpenProduct { get; }

        public ProductWindow() : this(null)
        {
        }

        public ProductWindow(Users? user)
        {
            CanOpenProduct = user is not null;
            InitializeComponent();

            SearchBox.TextChanged += (_, _) => ApplyFilters();
            PriceSort.SelectionChanged += (_, _) => ApplyFilters();

            FullUserName.Text = "Гость";

            if (user is not null)
            {
                ProductFilters.Visibility = Visibility.Visible;
                FullUserName.Text = $"{user.LastName} {user.FirstName} {user.Patronymic}".TrimEnd();
            }

            LoadProducts(DateOnly.FromDateTime(DateTime.Today));
        }

        private void LoadProducts(DateOnly calculationDate)
        {
            using var db = new AppDBContext();

            var orderedProductIds = ProductPricing.GetOrderedProductIds(db, calculationDate);

            var products = (
                from product in db.Products
                join subcategory in db.Subcategories on product.SubcategoryId equals subcategory.SubcategoryId
                join category in db.Categories on subcategory.CategoryId equals category.CategoryId
                join manufacturer in db.Manufacturers on product.ManufacturerId equals manufacturer.ManufacturerId
                let quantity = db.StockItems
                    .Where(stock => stock.ProductId == product.ProductId)
                    .Sum(stock => (int?)stock.AvailableQuantity) ?? 0
                orderby product.ProductName
                select new
                {
                    product.ProductId,
                    product.ProductName,
                    product.ImageFile,
                    product.Description,
                    product.Price,
                    HasDiscount = !orderedProductIds.Contains(product.ProductId),
                    Quantity = quantity,
                    category.CategoryName,
                    subcategory.SubcategoryName,
                    manufacturer.ManufacturerName
                }).ToList();

            _products = products.Select(product => new ProductCard
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Description = product.Description,
                OriginalPrice = product.Price,
                Price = ProductPricing.GetPrice(product.Price, product.HasDiscount),
                HasDiscount = product.HasDiscount,
                ManufacturerName = product.ManufacturerName,
                Quantity = product.Quantity,
                StockLabel = product.Quantity > 5 ? "много" : "мало",
                HasLowStock = product.Quantity <= 3,
                Category = $"{product.CategoryName} / {product.SubcategoryName}",
                ImagePath = $"/Boots;component/res/{product.ImageFile ?? "Чудо Обувь.png"}"
            }).ToList();

            ApplyFilters();
        }

        private void ProductCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenProduct((FrameworkElement)sender);
        }

        private void ProductDetails_Click(object sender, RoutedEventArgs e)
        {
            OpenProduct((FrameworkElement)sender);
        }

        private void OpenProduct(FrameworkElement source)
        {
            if (!CanOpenProduct || source.DataContext is not ProductCard product)
            {
                return;
            }

            try
            {
                var details = new ProductDetailsWindow { Owner = this };

                if (!details.LoadProduct(product.ProductId))
                {
                    _messages.ShowInfo("Товар больше недоступен в каталоге.");
                    return;
                }

                details.ShowDialog();
            }
            catch (SqlException)
            {
                _messages.ShowErr("Не удалось загрузить сведения о товаре. Проверь подключение к базе и повтори попытку.");
            }
        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            CurrentUserSession.CurrentUser = null;
            new MainWindow().Show();
            Close();
        }

        private sealed class ProductCard
        {
            public int ProductId { get; init; }
            public string ProductName { get; init; } = string.Empty;
            public string? Description { get; init; }
            public decimal OriginalPrice { get; init; }
            public decimal Price { get; init; }
            public bool HasDiscount { get; init; }
            public string ManufacturerName { get; init; } = string.Empty;
            public int Quantity { get; init; }
            public string StockLabel { get; init; } = string.Empty;
            public bool HasLowStock { get; init; }
            public string Category { get; init; } = string.Empty;
            public string ImagePath { get; init; } = string.Empty;
        }
    }
}
