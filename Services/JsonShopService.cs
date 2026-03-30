using System.Text.Json;
using FiresideEditorial.Models;

namespace FiresideEditorial.Services;

public class JsonShopService : IShopService
{
    private readonly IWebHostEnvironment _env;
    private List<ShopProduct>? _cache;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public JsonShopService(IWebHostEnvironment env) => _env = env;

    private async Task<List<ShopProduct>> LoadAsync()
    {
        if (_cache is not null) return _cache;
        var path = Path.Combine(_env.WebRootPath, "data", "products.json");
        if (!File.Exists(path)) return _cache = [];
        var json = await File.ReadAllTextAsync(path);
        _cache = JsonSerializer.Deserialize<List<ShopProduct>>(json, JsonOpts) ?? [];
        return _cache;
    }

    public async Task<List<ShopProduct>> GetAllProductsAsync() => await LoadAsync();

    public async Task<List<ShopProduct>> GetFeaturedProductsAsync()
    {
        var products = await LoadAsync();
        return products.Where(p => p.IsFeatured).ToList();
    }

    public async Task<List<ShopProduct>> GetProductsByCategoryAsync(string category)
    {
        var products = await LoadAsync();
        return products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
