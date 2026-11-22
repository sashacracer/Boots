namespace Boots.Models;

internal sealed record OrderDraftLine(
    int ProductId, string ProductName, int SizeId, int SizeValue,
    int Quantity, int AvailableQuantity, decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}
