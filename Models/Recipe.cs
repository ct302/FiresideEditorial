namespace FiresideEditorial.Models;

public class Recipe
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ImageAlt { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "Baking", "Drinks", "Dinner"
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public string Difficulty { get; set; } = "Easy"; // Easy, Medium, Advanced
    public List<RecipeIngredient> Ingredients { get; set; } = [];
    public List<string> Instructions { get; set; } = [];
    public List<string> Tips { get; set; } = [];
    public List<AffiliateProduct> RelatedProducts { get; set; } = [];
    public NutritionInfo? Nutrition { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RecipeIngredient
{
    public string Amount { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AffiliateUrl { get; set; }
}
