using System.Text.Json;
using FiresideEditorial.Models;
using Microsoft.EntityFrameworkCore;

namespace FiresideEditorial.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IWebHostEnvironment env)
    {
        var jsonPath = Path.Combine(env.WebRootPath, "data", "content.json");
        if (!File.Exists(jsonPath))
            return;

        var json = await File.ReadAllTextAsync(jsonPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var content = JsonSerializer.Deserialize<SeedContent>(json, options);

        if (content?.Cards is not { Count: > 0 })
            return;

        var existingSlugs = await db.Cards.Select(c => c.Slug).ToListAsync();
        var existingSet = new HashSet<string>(existingSlugs, StringComparer.OrdinalIgnoreCase);

        var newCards = content.Cards.Where(c => !existingSet.Contains(c.Slug)).ToList();
        if (newCards.Count > 0)
        {
            foreach (var card in newCards)
            {
                card.Id = 0;
                // Use date from content.json if present, otherwise fall back to now
                if (card.CreatedAt == default || card.CreatedAt == DateTime.MinValue)
                    card.CreatedAt = DateTime.UtcNow;
            }
            db.Cards.AddRange(newCards);
        }

        // Seed quote if none exists
        if (!await db.Quotes.AnyAsync() && content.Quote is not null)
        {
            content.Quote.Id = 0;
            db.Quotes.Add(content.Quote);
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync();
            Console.WriteLine($"Synced {newCards.Count} new cards from content.json (total: {await db.Cards.CountAsync()})");
        }
    }

    private class SeedContent
    {
        public List<EditorialCardModel> Cards { get; set; } = [];
        public QuoteModel Quote { get; set; } = new();
    }
}
