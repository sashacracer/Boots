namespace Boots
{
    public partial class ProductWindow
    {
        private void ApplyFilters()
        {
            var search = SearchBox.Text.Trim();
            IEnumerable<ProductCard> visibleProducts = _products;

            if (search.Length > 0)
            {
                visibleProducts = visibleProducts.Where(product =>
                    product.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    product.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
            }

            visibleProducts = PriceSort.SelectedIndex switch
            {
                1 => visibleProducts.OrderBy(product => product.Price),
                2 => visibleProducts.OrderByDescending(product => product.Price),
                _ => visibleProducts
            };

            ProductList.ItemsSource = visibleProducts.ToList();
        }
    }
}
