namespace FiresideEditorial.Models;

public class AffiliateProduct
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string AffiliateUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsEditorsPick { get; set; }
    public string Badge { get; set; } = string.Empty; // "Best Value", "Top Rated", etc.
    public List<string> Features { get; set; } = [];
}
