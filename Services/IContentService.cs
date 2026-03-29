using FiresideEditorial.Models;

namespace FiresideEditorial.Services;

public interface IContentService
{
    Task<List<EditorialCardModel>> GetCardsAsync();
    Task<EditorialCardModel?> GetCardBySlugAsync(string slug);
    Task<QuoteModel> GetQuoteAsync();
    Task SubmitTraditionAsync(TraditionSubmission submission);
    Task<List<SearchResult>> SearchAsync(string query);
}

public class SearchResult
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
