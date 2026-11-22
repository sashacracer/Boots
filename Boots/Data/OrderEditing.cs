using Microsoft.EntityFrameworkCore;

namespace Boots.Data;

internal static class OrderEditing
{
    public static bool UpdateDate(AppDBContext db, int orderId, DateOnly date)
    {
        return db.Orders.Where(order => order.OrderId == orderId)
            .ExecuteUpdate(setters => setters.SetProperty(order => order.OrderDate, date)) == 1;
    }

    public static bool DeleteItem(AppDBContext db, int orderId, int orderItemId)
    {
        using var transaction = db.Database.BeginTransaction();

        if (OrderDeletion.GetOrderForUpdate(db, orderId) is null)
        {
            return false;
        }

        var item = db.OrderItems.SingleOrDefault(item =>
            item.OrderId == orderId && item.OrderItemId == orderItemId);

        if (item is null)
        {
            return false;
        }

        OrderDeletion.ReturnStock(db, item.ProductId, item.SizeId, item.Quantity);
        db.OrderItems.Remove(item);
        db.SaveChanges();

        transaction.Commit();
        return true;
    }
}
