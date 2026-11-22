using Boots.Data;
using Boots.helper;
using Boots.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;

namespace Boots;

public partial class OrderCreateWindow : Window
{
    private readonly Users _operator;
    private readonly Helper _messages = new();
    private readonly ObservableCollection<OrderDraftLine> _items = [];

    public OrderCreateWindow(Users orderOperator)
    {
        _operator = orderOperator;
        InitializeComponent();

        using var db = new AppDBContext();

        var customers = db.Users
            .Where(user => user.RoleId == 3)
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ToList();

        CustomerBox.ItemsSource = customers.Select(user => new
        {
            user.UserId,
            FullName = $"{user.LastName} {user.FirstName} {user.Patronymic}".TrimEnd()
        }).ToList();

        DraftGrid.ItemsSource = _items;
        UpdateTotal();
    }

    public int CreatedOrderId { get; private set; }

    private void AddProduct_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var catalog = new ProductWindow(_operator, selectForOrder: true) { Owner = this };

            if (catalog.ShowDialog() != true || catalog.SelectedOrderLine is not { } line)
            {
                return;
            }

            var existing = _items.FirstOrDefault(item => item.ProductId == line.ProductId && item.SizeId == line.SizeId);
            var existingQuantity = existing?.Quantity ?? 0;
            if (line.Quantity > line.AvailableQuantity - existingQuantity)
            {
                _messages.ShowWarning($"Этого размера доступно {line.AvailableQuantity} пар с учётом уже добавленных позиций.");
                return;
            }

            if (existing is null)
            {
                _items.Add(line);
            }
            else
            {
                _items[_items.IndexOf(existing)] = line with { Quantity = existingQuantity + line.Quantity };
            }

            UpdateTotal();
        }
        catch (SqlException)
        {
            _messages.ShowErr("Не удалось открыть каталог. Проверь подключение к базе и повтори попытку.");
        }
    }

    private void RemoveProduct_Click(object sender, RoutedEventArgs e)
    {
        if (DraftGrid.SelectedItem is OrderDraftLine line)
        {
            _items.Remove(line);
            UpdateTotal();
        }
    }

    private void ConfirmOrder_Click(object sender, RoutedEventArgs e)
    {
        if (CustomerBox.SelectedValue is not int customerId || _items.Count == 0)
        {
            _messages.ShowWarning("Выбери клиента и добавь хотя бы одну позицию.");
            return;
        }

        if (!_messages.Confirm($"Сохранить заказ на сумму {_items.Sum(line => line.LineTotal):N2} ₽?"))
        {
            return;
        }

        try
        {
            using var db = new AppDBContext();
            CreatedOrderId = OrderCreation.Save(db, customerId, _items);
            DialogResult = true;
        }
        catch (InvalidOperationException exception)
        {
            _messages.ShowWarning(exception.Message);
        }
        catch (Exception exception) when (exception is SqlException or DbUpdateException)
        {
            _messages.ShowErr("Не удалось сохранить заказ. Проверь подключение к базе и наличие товара, затем повтори попытку.");
        }
    }

    private void UpdateTotal()
    {
        DraftTotal.Text = $"Итого: {_items.Sum(line => line.LineTotal):N2} ₽";
    }
}
