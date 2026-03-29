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

    public async Task<List<SearchResult>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return [];

        var cards = await GetCardsAsync();
        var terms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var results = new List<SearchResult>();

        foreach (var card in cards)
        {
            var titleMatch = terms.All(t => card.Title.Contains(t, StringComparison.OrdinalIgnoreCase));
            var descMatch = terms.All(t => card.Description.Contains(t, StringComparison.OrdinalIgnoreCase));
            var catMatch = terms.All(t => card.Category.Contains(t, StringComparison.OrdinalIgnoreCase));

            // Search article markdown content
            var mdPath = Path.Combine(_env.WebRootPath, "data", "articles", $"{card.Slug}.md");
            var bodyMatch = false;
            var snippet = card.Description;

            if (File.Exists(mdPath))
            {
                var markdown = await File.ReadAllTextAsync(mdPath);
                bodyMatch = terms.All(t => markdown.Contains(t, StringComparison.OrdinalIgnoreCase));

                if (bodyMatch && !titleMatch && !descMatch)
                {
                    // Extract a snippet around the first match
                    var idx = markdown.IndexOf(terms[0], StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        var start = Math.Max(0, idx - 60);
                        var length = Math.Min(160, markdown.Length - start);
                        snippet = (start > 0 ? "..." : "") + markdown.Substring(start, length).Trim() + "...";
                        // Strip markdown formatting from snippet
                        snippet = System.Text.RegularExpressions.Regex.Replace(snippet, @"[#*_\[\]>]", "");
                    }
                }
            }

            if (titleMatch || descMatch || catMatch || bodyMatch)
            {
                results.Add(new SearchResult
                {
                    Title = card.Title,
                    Slug = card.Slug,
                    Category = card.Category,
                    Snippet = snippet,
                    ImageUrl = card.ImageUrl
                });
            }
        }

        return results;
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
