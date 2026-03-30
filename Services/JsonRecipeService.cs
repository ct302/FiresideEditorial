using System.Text.Json;
using FiresideEditorial.Models;

namespace FiresideEditorial.Services;

public class JsonRecipeService : IRecipeService
{
    private readonly IWebHostEnvironment _env;
    private List<Recipe>? _cache;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public JsonRecipeService(IWebHostEnvironment env) => _env = env;

    private async Task<List<Recipe>> LoadAsync()
    {
        if (_cache is not null) return _cache;
        var path = Path.Combine(_env.WebRootPath, "data", "recipes.json");
        if (!File.Exists(path)) return _cache = [];
        var json = await File.ReadAllTextAsync(path);
        _cache = JsonSerializer.Deserialize<List<Recipe>>(json, JsonOpts) ?? [];
        return _cache;
    }

    public async Task<List<Recipe>> GetAllRecipesAsync() => await LoadAsync();

    public async Task<Recipe?> GetRecipeBySlugAsync(string slug)
    {
        var recipes = await LoadAsync();
        return recipes.FirstOrDefault(r => r.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<Recipe>> GetRecipesByCategoryAsync(string category)
    {
        var recipes = await LoadAsync();
        return recipes.Where(r => r.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
