namespace Boots.Data;

internal static class ProductPricing
{
    public static IQueryable<int> GetOrderedProductIds(AppDBContext db, DateOnly calculationDate)
    {
        var currentMonthStart = new DateOnly(calculationDate.Year, calculationDate.Month, 1);
        var previousMonthStart = currentMonthStart.AddMonths(-1);

        return from item in db.OrderItems
               join order in db.Orders on item.OrderId equals order.OrderId
               where order.OrderDate >= previousMonthStart && order.OrderDate < currentMonthStart
               select item.ProductId;
    }

    public static decimal GetPrice(decimal originalPrice, bool hasDiscount)
    {
        return hasDiscount
            ? decimal.Round(originalPrice * 0.75m, 2, MidpointRounding.AwayFromZero)
            : originalPrice;
    }
}
