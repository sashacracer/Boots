using Boots.Data;
using Boots.helper;
using Boots.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace Boots;

public partial class OrdersWindow : Window
{
    private readonly Helper _messages = new();
    private readonly Users? _orderOperator;

    public OrdersWindow() : this(null)
    {
    }

    internal OrdersWindow(Users? orderOperator)
    {
        _orderOperator = orderOperator;
        InitializeComponent();

        if (_orderOperator?.RoleId is 1 or 2)
        {
            AddOrderButton.Visibility = Visibility.Visible;
            DeleteOrderButton.Visibility = Visibility.Visible;
        }

        LoadOrders();
    }

    internal bool OrdersChanged { get; private set; }

    private void LoadOrders()
    {
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

        EmptyOrdersText.Visibility = orders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_orderOperator is not { RoleId: 1 or 2 })
        {
            return;
        }

        try
        {
            var createOrder = new OrderCreateWindow(_orderOperator) { Owner = this };
            if (createOrder.ShowDialog() == true)
            {
                OrdersChanged = true;
                LoadOrders();
                OrdersGrid.SelectedValue = createOrder.CreatedOrderId;
            }
        }
        catch (SqlException)
        {
            _messages.ShowErr("Не удалось открыть форму заказа или обновить список. Проверь подключение к базе.");
        }
    }

    private void DeleteOrder_Click(object sender, RoutedEventArgs e)
    {
        if (_orderOperator is not { RoleId: 1 or 2 } || OrdersGrid.SelectedValue is not int orderId)
        {
            return;
        }

        if (!_messages.Confirm($"Удалить заказ № {orderId}? Пары из заказа вернутся на склад. Удаление нельзя отменить."))
        {
            return;
        }

        var deleted = false;
        try
        {
            using var db = new AppDBContext();
            deleted = OrderDeletion.Delete(db, orderId);
            OrdersChanged = true;
            LoadOrders();

            if (!deleted)
            {
                _messages.ShowInfo("Этот заказ уже удалён. Список обновлён.");
            }
        }
        catch (Exception exception) when (exception is SqlException or DbUpdateException
            or InvalidOperationException or OverflowException)
        {
            _messages.ShowErr(deleted
                ? "Заказ удалён, но список не удалось обновить. Закрой и открой окно заказов."
                : "Не удалось удалить заказ и вернуть остатки. Проверь подключение к базе и актуальность заказа.");
        }
    }

    private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source != OrdersGrid)
        {
            return;
        }

        DeleteOrderButton.IsEnabled = OrdersGrid.SelectedValue is int;
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
