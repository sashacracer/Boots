using Boots.Data;
using Boots.Models;
using Boots.Static;
using System.Windows;

namespace Boots
{
    public partial class ProductWindow : Window
    {
        private List<ProductCard> _products = [];

        public ProductWindow()
        {
            InitializeComponent();

            SearchBox.TextChanged += (_, _) => ApplyFilters();
            PriceSort.SelectionChanged += (_, _) => ApplyFilters();

            FullUserName.Text = "Гость";
            LoadProducts(DateOnly.FromDateTime(DateTime.Today));
        }

        public ProductWindow(Users? user) : this()
        {
            if (user is not null)
            {
                ProductFilters.Visibility = Visibility.Visible;
                FullUserName.Text = $"{user.LastName} {user.FirstName} {user.Patronymic}".TrimEnd();
            }
        }

        private void LoadProducts(DateOnly calculationDate)
        {
            using var db = new AppDBContext();

            var currentMonthStart = new DateOnly(calculationDate.Year, calculationDate.Month, 1);
            var previousMonthStart = currentMonthStart.AddMonths(-1);

            var orderedProductIds =
                from item in db.OrderItems
                join order in db.Orders on item.OrderId equals order.OrderId
                where order.OrderDate >= previousMonthStart && order.OrderDate < currentMonthStart
                select item.ProductId;

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
                ProductName = product.ProductName,
                Description = product.Description,
                OriginalPrice = product.Price,
                Price = product.HasDiscount
                    ? decimal.Round(product.Price * 0.75m, 2, MidpointRounding.AwayFromZero)
                    : product.Price,
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

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            CurrentUserSession.CurrentUser = null;
            new MainWindow().Show();
            Close();
        }

        private sealed class ProductCard
        {
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
