using FiresideEditorial.Models;

namespace FiresideEditorial.Services;

public interface IContentService
{
    Task<List<EditorialCardModel>> GetCardsAsync();
    Task<EditorialCardModel?> GetCardBySlugAsync(string slug);
    Task<QuoteModel> GetQuoteAsync();
    Task SubmitTraditionAsync(TraditionSubmission submission);
}
