using Boots.Models;
using Microsoft.EntityFrameworkCore;

namespace Boots.Data;

internal static class OrderDeletion
{
    public static bool Delete(AppDBContext db, int orderId)
    {
        using var transaction = db.Database.BeginTransaction();

        var order = GetOrderForUpdate(db, orderId);
        if (order is null)
        {
            return false;
        }

        var items = db.OrderItems.Where(item => item.OrderId == orderId).ToList();
        var stockToReturn = items.GroupBy(item => (item.ProductId, item.SizeId))
            .OrderBy(group => group.Key.ProductId)
            .ThenBy(group => group.Key.SizeId);

        foreach (var group in stockToReturn)
        {
            ReturnStock(db, group.Key.ProductId, group.Key.SizeId, group.Sum(item => item.Quantity));
        }

        db.OrderItems.RemoveRange(items);
        db.SaveChanges();

        db.Orders.Remove(order);
        db.SaveChanges();

        transaction.Commit();
        return true;
    }

    internal static Orders? GetOrderForUpdate(AppDBContext db, int orderId)
    {
        // Order-level locking keeps edits and deletions from returning the same stock twice.
        return db.Orders
            .FromSqlInterpolated($"""
                SELECT OrderId, OrderDate, UserId
                FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK)
                WHERE OrderId = {orderId}
                """)
            .SingleOrDefault();
    }

    internal static void ReturnStock(AppDBContext db, int productId, int sizeId, int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("В заказе указано некорректное количество пар.");
        }

        var stock = db.StockItems
            .FromSqlInterpolated($"""
                SELECT StockItemId, ProductId, SizeId, AvailableQuantity
                FROM dbo.StockItems WITH (UPDLOCK, HOLDLOCK)
                WHERE ProductId = {productId} AND SizeId = {sizeId}
                """)
            .OrderBy(item => item.StockItemId)
            .FirstOrDefault();

        if (stock is null)
        {
            db.StockItems.Add(new StockItems
            {
                ProductId = productId,
                SizeId = sizeId,
                AvailableQuantity = quantity
            });
        }
        else
        {
            stock.AvailableQuantity = checked(stock.AvailableQuantity + quantity);
        }
    }
}
