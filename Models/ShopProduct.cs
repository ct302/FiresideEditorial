namespace FiresideEditorial.Models;

public class NutritionInfo
{
    public int Calories { get; set; }
    public string Fat { get; set; } = string.Empty;
    public string Carbs { get; set; } = string.Empty;
    public string Protein { get; set; } = string.Empty;
    public string Sugar { get; set; } = string.Empty;
}

public class ShopProduct
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string GumroadUrl { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "Printable", "Kit", "Booklet"
    public string Badge { get; set; } = string.Empty; // "New", "Bestseller", "Free"
    public List<string> Features { get; set; } = [];
    public bool IsFeatured { get; set; }
}
