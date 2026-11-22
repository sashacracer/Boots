using Boots.Models;

namespace Boots.Data;

internal static class ProductCreation
{
    public static int Save(AppDBContext db, Products product, IReadOnlyDictionary<int, int> quantities)
    {
        using var transaction = db.Database.BeginTransaction();

        db.Products.Add(product);
        db.SaveChanges();

        db.StockItems.AddRange(quantities.Select(stock => new StockItems
        {
            ProductId = product.ProductId,
            SizeId = stock.Key,
            AvailableQuantity = stock.Value
        }));

        db.SaveChanges();
        transaction.Commit();
        return product.ProductId;
    }
}
