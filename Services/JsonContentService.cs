using System.Text.Json;
using FiresideEditorial.Models;
using Markdig;

namespace FiresideEditorial.Services;

public class JsonContentService : IContentService
{
    private readonly IWebHostEnvironment _env;
    private readonly MarkdownPipeline _mdPipeline;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JsonContentService(IWebHostEnvironment env)
    {
        _env = env;
        _mdPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
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
        var card = cards.FirstOrDefault(c => c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        if (card is not null)
        {
            card.ArticleBody = await LoadArticleBodyAsync(card.Slug);
        }

        return card;
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
        Console.WriteLine($"New tradition from {submission.Name}: {submission.Story}");
        return Task.CompletedTask;
    }

    private async Task<string> LoadArticleBodyAsync(string slug)
    {
        var mdPath = Path.Combine(_env.WebRootPath, "data", "articles", $"{slug}.md");

        if (!File.Exists(mdPath))
            return "<p>Article content coming soon.</p>";

        var markdown = await File.ReadAllTextAsync(mdPath);
        return Markdown.ToHtml(markdown, _mdPipeline);
    }

    private class ContentFile
    {
        public List<EditorialCardModel> Cards { get; set; } = [];
        public QuoteModel Quote { get; set; } = new();
    }
}
