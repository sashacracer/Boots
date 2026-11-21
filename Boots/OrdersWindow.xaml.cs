using Boots.Data;
using Boots.helper;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Boots;

public partial class OrdersWindow : Window
{
    private readonly Helper _messages = new();

    public OrdersWindow()
    {
        InitializeComponent();

        using var db = new AppDBContext();

        var orders = (
            from order in db.Orders
            join customer in db.Users on order.UserId equals customer.UserId
            orderby order.OrderDate descending, order.OrderId descending
            select new
            {
                order.OrderId,
                order.OrderDate,
                customer.LastName,
                customer.FirstName,
                customer.Patronymic
            }).ToList();

        OrdersGrid.ItemsSource = orders.Select(order => new
        {
            order.OrderId,
            order.OrderDate,
            CustomerName = $"{order.LastName} {order.FirstName} {order.Patronymic}".TrimEnd()
        }).ToList();

        if (orders.Count == 0)
        {
            EmptyOrdersText.Visibility = Visibility.Visible;
        }
    }

    private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source != OrdersGrid)
        {
            return;
        }

        OrderItemsGrid.ItemsSource = null;

        if (OrdersGrid.SelectedValue is not int orderId)
        {
            OrderTotalText.Text = "Выбери заказ в верхней таблице.";
            return;
        }

        try
        {
            using var db = new AppDBContext();

            var items = (
                from item in db.OrderItems
                join product in db.Products on item.ProductId equals product.ProductId
                join manufacturer in db.Manufacturers on product.ManufacturerId equals manufacturer.ManufacturerId
                join size in db.Sizes on item.SizeId equals size.SizeId
                where item.OrderId == orderId
                orderby item.OrderItemId
                select new
                {
                    product.ProductName,
                    manufacturer.ManufacturerName,
                    size.SizeValue,
                    item.Quantity,
                    item.UnitPrice
                }).ToList();

            OrderItemsGrid.ItemsSource = items;

            var total = items.Sum(item => item.UnitPrice * item.Quantity);
            OrderTotalText.Text = $"Итого: {total:N2} ₽";
        }
        catch (SqlException)
        {
            OrderTotalText.Text = "Состав заказа недоступен.";
            _messages.ShowErr("Не удалось загрузить позиции заказа. Проверь подключение к базе и повтори попытку.");
        }
    }
}
