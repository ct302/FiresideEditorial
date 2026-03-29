using System.Text.Json;
using FiresideEditorial.Models;
using Microsoft.EntityFrameworkCore;

namespace FiresideEditorial.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IWebHostEnvironment env)
    {
        // Only seed if the Cards table is empty
        if (await db.Cards.AnyAsync())
            return;

        var jsonPath = Path.Combine(env.WebRootPath, "data", "content.json");
        if (!File.Exists(jsonPath))
            return;

        var json = await File.ReadAllTextAsync(jsonPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var content = JsonSerializer.Deserialize<SeedContent>(json, options);

        if (content?.Cards is { Count: > 0 })
        {
            foreach (var card in content.Cards)
            {
                card.Id = 0; // Let DB assign IDs
                card.CreatedAt = DateTime.UtcNow;
            }
            db.Cards.AddRange(content.Cards);
        }

        if (content?.Quote is not null)
        {
            content.Quote.Id = 0;
            db.Quotes.Add(content.Quote);
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"Seeded {content?.Cards?.Count ?? 0} cards and 1 quote from content.json");
    }

    private class SeedContent
    {
        public List<EditorialCardModel> Cards { get; set; } = [];
        public QuoteModel Quote { get; set; } = new();
    }
}
