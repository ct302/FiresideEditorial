using FiresideEditorial.Models;

namespace FiresideEditorial.Services;

public interface IShopService
{
    Task<List<ShopProduct>> GetAllProductsAsync();
    Task<List<ShopProduct>> GetFeaturedProductsAsync();
    Task<List<ShopProduct>> GetProductsByCategoryAsync(string category);
}
