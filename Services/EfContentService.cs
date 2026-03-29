using FiresideEditorial.Data;
using FiresideEditorial.Models;
using Markdig;
using Microsoft.EntityFrameworkCore;

namespace FiresideEditorial.Services;

public class EfContentService : IContentService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly MarkdownPipeline _mdPipeline;

    public EfContentService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
        _mdPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public async Task<List<EditorialCardModel>> GetCardsAsync()
    {
        return await _db.Cards
            .OrderBy(c => c.Id)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<EditorialCardModel?> GetCardBySlugAsync(string slug)
    {
        var card = await _db.Cards
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug);

        if (card is not null)
            card.ArticleBody = await LoadArticleBodyAsync(card.Slug);

        return card;
    }

    public async Task<QuoteModel> GetQuoteAsync()
    {
        return await _db.Quotes
            .AsNoTracking()
            .FirstOrDefaultAsync() ?? new QuoteModel();
    }

    public async Task SubmitTraditionAsync(TraditionSubmission submission)
    {
        submission.SubmittedAt = DateTime.UtcNow;
        _db.Traditions.Add(submission);
        await _db.SaveChangesAsync();
    }

    public async Task<List<SearchResult>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return [];

        var terms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cards = await _db.Cards.AsNoTracking().ToListAsync();
        var results = new List<SearchResult>();

        foreach (var card in cards)
        {
            var titleMatch = terms.All(t => card.Title.Contains(t, StringComparison.OrdinalIgnoreCase));
            var descMatch = terms.All(t => card.Description.Contains(t, StringComparison.OrdinalIgnoreCase));
            var catMatch = terms.All(t => card.Category.Contains(t, StringComparison.OrdinalIgnoreCase));

            var mdPath = Path.Combine(_env.WebRootPath, "data", "articles", $"{card.Slug}.md");
            var bodyMatch = false;
            var snippet = card.Description;

            if (File.Exists(mdPath))
            {
                var markdown = await File.ReadAllTextAsync(mdPath);
                bodyMatch = terms.All(t => markdown.Contains(t, StringComparison.OrdinalIgnoreCase));

                if (bodyMatch && !titleMatch && !descMatch)
                {
                    var idx = markdown.IndexOf(terms[0], StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        var start = Math.Max(0, idx - 60);
                        var length = Math.Min(160, markdown.Length - start);
                        snippet = (start > 0 ? "..." : "") + markdown.Substring(start, length).Trim() + "...";
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
}
