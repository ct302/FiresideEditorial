using System.Text.Json;
using FiresideEditorial.Models;

namespace FiresideEditorial.Services;

public class JsonContentService : IContentService
{
    private readonly IWebHostEnvironment _env;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JsonContentService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<List<EditorialCardModel>> GetCardsAsync()
    {
        var path = Path.Combine(_env.WebRootPath, "data", "content.json");
        var json = await File.ReadAllTextAsync(path);
        var content = JsonSerializer.Deserialize<ContentFile>(json, _jsonOptions);
        return content?.Cards ?? [];
    }

    public async Task<EditorialCardModel?> GetCardBySlugAsync(string slug)
    {
        var cards = await GetCardsAsync();
        return cards.FirstOrDefault(c => c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<QuoteModel> GetQuoteAsync()
    {
        var path = Path.Combine(_env.WebRootPath, "data", "content.json");
        var json = await File.ReadAllTextAsync(path);
        var content = JsonSerializer.Deserialize<ContentFile>(json, _jsonOptions);
        return content?.Quote ?? new QuoteModel();
    }

    public Task SubmitTraditionAsync(TraditionSubmission submission)
    {
        // TODO: Persist to database or file
        Console.WriteLine($"New tradition from {submission.Name}: {submission.Story}");
        return Task.CompletedTask;
    }

    private class ContentFile
    {
        public List<EditorialCardModel> Cards { get; set; } = [];
        public QuoteModel Quote { get; set; } = new();
    }
}
