using FiresideEditorial.Models;

namespace FiresideEditorial.Services;

public interface IContentService
{
    Task<List<EditorialCardModel>> GetCardsAsync();
    Task<QuoteModel> GetQuoteAsync();
    Task SubmitTraditionAsync(TraditionSubmission submission);
}
