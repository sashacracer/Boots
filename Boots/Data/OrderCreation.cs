using Boots.Models;
using Microsoft.EntityFrameworkCore;

namespace Boots.Data;

internal static class OrderCreation
{
    public static int Save(AppDBContext db, int customerId, IReadOnlyList<OrderDraftLine> lines)
    {
        if (lines.Count == 0 || lines.Any(line => line.Quantity <= 0))
        {
            throw new InvalidOperationException("Добавь в заказ хотя бы одну позицию с положительным количеством.");
        }

        using var transaction = db.Database.BeginTransaction();

        if (!db.Users.Any(user => user.UserId == customerId && user.RoleId == 3))
        {
            throw new InvalidOperationException("Выбранный клиент больше недоступен. Выбери другого клиента.");
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var orderedProductIds = ProductPricing.GetOrderedProductIds(db, today);

        foreach (var line in lines.OrderBy(item => item.ProductId).ThenBy(item => item.SizeId))
        {
            var product = db.Products
                .Where(item => item.ProductId == line.ProductId)
                .Select(item => new { item.Price, HasDiscount = !orderedProductIds.Contains(item.ProductId) })
                .SingleOrDefault();

            if (product is null)
            {
                throw new InvalidOperationException($"Товар «{line.ProductName}» больше недоступен.");
            }

            if (ProductPricing.GetPrice(product.Price, product.HasDiscount) != line.UnitPrice)
            {
                throw new InvalidOperationException(
                    $"Цена товара «{line.ProductName}» изменилась. Удали позицию из заказа и добавь её заново.");
            }

            ReserveStock(db, line);
        }

        var order = new Orders { UserId = customerId, OrderDate = today };
        db.Orders.Add(order);
        db.SaveChanges();

        db.OrderItems.AddRange(lines.Select(line => new OrderItems
        {
            OrderId = order.OrderId,
            ProductId = line.ProductId,
            SizeId = line.SizeId,
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice
        }));

        db.SaveChanges();
        transaction.Commit();
        return order.OrderId;
    }

    private static void ReserveStock(AppDBContext db, OrderDraftLine line)
    {
        var stockRows = db.StockItems
            .Where(stock => stock.ProductId == line.ProductId && stock.SizeId == line.SizeId)
            .OrderBy(stock => stock.StockItemId)
            .Select(stock => new { stock.StockItemId, stock.AvailableQuantity })
            .ToList();

        var remaining = line.Quantity;

        foreach (var stock in stockRows)
        {
            var quantity = Math.Min(remaining, stock.AvailableQuantity);
            if (quantity <= 0)
            {
                continue;
            }

            // The predicate also protects against another order using this stock row.
            var updated = db.StockItems
                .Where(item => item.StockItemId == stock.StockItemId && item.AvailableQuantity >= quantity)
                .ExecuteUpdate(setters => setters.SetProperty(
                    item => item.AvailableQuantity, item => item.AvailableQuantity - quantity));

            if (updated == 0)
            {
                break;
            }

            remaining -= quantity;
            if (remaining == 0)
            {
                return;
            }
        }

        throw new InvalidOperationException(
            $"Для товара «{line.ProductName}», размер {line.SizeValue}, недостаточно пар. Проверь остаток и количество.");
    }
}
