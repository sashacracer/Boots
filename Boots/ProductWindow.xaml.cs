using Boots.Data;
using Boots.Models;
using Boots.Static;
using System.Windows;

namespace Boots
{
    public partial class ProductWindow : Window
    {
        public ProductWindow()
        {
            InitializeComponent();

            FullUserName.Text = "Гость";
            LoadProducts();
        }

        public ProductWindow(Users? user) : this()
        {
            if (user is not null)
            {
                FullUserName.Text = $"{user.LastName} {user.FirstName} {user.Patronymic}".TrimEnd();
            }
        }

        private void LoadProducts()
        {
            using var db = new AppDBContext();

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
                    Quantity = quantity,
                    category.CategoryName,
                    subcategory.SubcategoryName,
                    manufacturer.ManufacturerName
                }).ToList();

            ProductList.ItemsSource = products.Select(product => new
            {
                product.ProductName,
                product.Description,
                product.Price,
                product.ManufacturerName,
                product.Quantity,
                StockLabel = product.Quantity > 5 ? "много" : "мало",
                HasLowStock = product.Quantity <= 3,
                Category = $"{product.CategoryName} / {product.SubcategoryName}",
                ImagePath = $"/Boots;component/res/{product.ImageFile ?? "Чудо Обувь.png"}"
            }).ToList();
        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            CurrentUserSession.CurrentUser = null;
            new MainWindow().Show();
            Close();
        }
    }
}
