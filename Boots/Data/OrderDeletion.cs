using Microsoft.EntityFrameworkCore;

namespace Boots.Data;

internal static class OrderDeletion
{
    public static bool Delete(AppDBContext db, int orderId)
    {
        using var transaction = db.Database.BeginTransaction();

        // A second delete must wait for this order before it can return its stock.
        var order = db.Orders
            .FromSqlInterpolated($"""
                SELECT OrderId, OrderDate, UserId
                FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK)
                WHERE OrderId = {orderId}
                """)
            .SingleOrDefault();

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
            var quantity = group.Sum(item => item.Quantity);
            if (quantity <= 0)
            {
                throw new InvalidOperationException("В заказе указано некорректное количество пар.");
            }

            var stock = db.StockItems
                .FromSqlInterpolated($"""
                    SELECT StockItemId, ProductId, SizeId, AvailableQuantity
                    FROM dbo.StockItems WITH (UPDLOCK, HOLDLOCK)
                    WHERE ProductId = {group.Key.ProductId} AND SizeId = {group.Key.SizeId}
                    """)
                .OrderBy(item => item.StockItemId)
                .FirstOrDefault();

            if (stock is null)
            {
                db.StockItems.Add(new Models.StockItems
                {
                    ProductId = group.Key.ProductId,
                    SizeId = group.Key.SizeId,
                    AvailableQuantity = quantity
                });
            }
            else
            {
                stock.AvailableQuantity = checked(stock.AvailableQuantity + quantity);
            }
        }

        db.OrderItems.RemoveRange(items);
        db.SaveChanges();

        db.Orders.Remove(order);
        db.SaveChanges();

        transaction.Commit();
        return true;
    }
}
