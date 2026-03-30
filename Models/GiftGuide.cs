namespace FiresideEditorial.Models;

public class GiftGuide
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ImageAlt { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "Budget", "Recipient", "Theme"
    public string PriceRange { get; set; } = string.Empty; // "Under $25", "$25-$50", etc.
    public List<AffiliateProduct> Products { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
