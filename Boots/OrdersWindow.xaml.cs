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

    private bool CanEditOrders => _orderOperator?.RoleId == 1;

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

        OrderEditPanel.Visibility = CanEditOrders ? Visibility.Visible : Visibility.Collapsed;
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

        OrdersGrid.ItemsSource = orders.Select(order => new OrderSummary(
            order.OrderId,
            order.OrderDate,
            $"{order.LastName} {order.FirstName} {order.Patronymic}".TrimEnd())).ToList();

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

    private void SaveDate_Click(object sender, RoutedEventArgs e)
    {
        if (!CanEditOrders || OrdersGrid.SelectedValue is not int orderId)
        {
            return;
        }

        if (OrderDatePicker.SelectedDate is not DateTime selectedDate)
        {
            _messages.ShowWarning("Выбери дату заказа.");
            return;
        }

        var saved = false;
        try
        {
            using var db = new AppDBContext();
            saved = OrderEditing.UpdateDate(db, orderId, DateOnly.FromDateTime(selectedDate));
            RefreshEditedOrder(orderId);

            if (!saved)
            {
                _messages.ShowInfo("Заказ больше недоступен. Список обновлён.");
            }
        }
        catch (SqlException)
        {
            _messages.ShowErr(saved
                ? "Дата сохранена, но список не удалось обновить. Открой окно заказов заново."
                : "Не удалось сохранить дату заказа. Проверь подключение к базе и повтори попытку.");
        }
    }

    private void DeleteOrderItem_Click(object sender, RoutedEventArgs e)
    {
        if (!CanEditOrders || OrdersGrid.SelectedValue is not int orderId ||
            OrderItemsGrid.SelectedValue is not int orderItemId)
        {
            return;
        }

        if (!_messages.Confirm("Удалить выбранную позицию из заказа? Пары вернутся на склад."))
        {
            return;
        }

        var deleted = false;
        try
        {
            using var db = new AppDBContext();
            deleted = OrderEditing.DeleteItem(db, orderId, orderItemId);
            RefreshEditedOrder(orderId);

            if (!deleted)
            {
                _messages.ShowInfo("Эта позиция уже недоступна. Список обновлён.");
            }
        }
        catch (Exception exception) when (exception is SqlException or DbUpdateException
            or InvalidOperationException or OverflowException)
        {
            _messages.ShowErr(deleted
                ? "Позиция удалена, но список не удалось обновить. Открой окно заказов заново."
                : "Не удалось удалить позицию и вернуть пары на склад. Проверь состояние заказа.");
        }
    }

    private void RefreshEditedOrder(int orderId)
    {
        OrdersChanged = true;
        OrdersGrid.SelectedIndex = -1;
        LoadOrders();
        OrdersGrid.SelectedValue = orderId;
    }

    private void OrderItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source == OrderItemsGrid)
        {
            DeleteOrderItemButton.IsEnabled = OrderItemsGrid.SelectedValue is int;
        }
    }

    private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source != OrdersGrid)
        {
            return;
        }

        var order = OrdersGrid.SelectedItem as OrderSummary;
        DeleteOrderButton.IsEnabled = order is not null;
        OrderEditPanel.IsEnabled = order is not null;
        OrderDatePicker.SelectedDate = order?.OrderDate.ToDateTime(TimeOnly.MinValue);
        DeleteOrderItemButton.IsEnabled = false;
        OrderItemsGrid.ItemsSource = null;

        if (order is null)
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
                where item.OrderId == order.OrderId
                orderby item.OrderItemId
                select new
                {
                    item.OrderItemId,
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

    private sealed record OrderSummary(int OrderId, DateOnly OrderDate, string CustomerName);
}
