using System.Text.Json;
using FiresideEditorial.Models;

namespace FiresideEditorial.Services;

public class JsonGiftGuideService : IGiftGuideService
{
    private readonly IWebHostEnvironment _env;
    private List<GiftGuide>? _cache;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public JsonGiftGuideService(IWebHostEnvironment env) => _env = env;

    private async Task<List<GiftGuide>> LoadAsync()
    {
        if (_cache is not null) return _cache;
        var path = Path.Combine(_env.WebRootPath, "data", "gift-guides.json");
        if (!File.Exists(path)) return _cache = [];
        var json = await File.ReadAllTextAsync(path);
        _cache = JsonSerializer.Deserialize<List<GiftGuide>>(json, JsonOpts) ?? [];
        return _cache;
    }

    public async Task<List<GiftGuide>> GetAllGuidesAsync() => await LoadAsync();

    public async Task<GiftGuide?> GetGuideBySlugAsync(string slug)
    {
        var guides = await LoadAsync();
        return guides.FirstOrDefault(g => g.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<GiftGuide>> GetGuidesByCategoryAsync(string category)
    {
        var guides = await LoadAsync();
        return guides.Where(g => g.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
