using FiresideEditorial.Models;

namespace FiresideEditorial.Services;

public interface IGiftGuideService
{
    Task<List<GiftGuide>> GetAllGuidesAsync();
    Task<GiftGuide?> GetGuideBySlugAsync(string slug);
    Task<List<GiftGuide>> GetGuidesByCategoryAsync(string category);
}
