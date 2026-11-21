using Boots.Data;
using System.Windows;

namespace Boots;

public partial class OrdersWindow : Window
{
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
                order.OrderDate,
                customer.LastName,
                customer.FirstName,
                customer.Patronymic
            }).ToList();

        OrdersGrid.ItemsSource = orders.Select(order => new
        {
            order.OrderDate,
            CustomerName = $"{order.LastName} {order.FirstName} {order.Patronymic}".TrimEnd()
        }).ToList();

        if (orders.Count == 0)
        {
            EmptyOrdersText.Visibility = Visibility.Visible;
        }
    }
}
