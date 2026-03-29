using FiresideEditorial.Models;
using Microsoft.EntityFrameworkCore;

namespace FiresideEditorial.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<EditorialCardModel> Cards => Set<EditorialCardModel>();
    public DbSet<QuoteModel> Quotes => Set<QuoteModel>();
    public DbSet<TraditionSubmission> Traditions => Set<TraditionSubmission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EditorialCardModel>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
            e.Property(c => c.CreatedAt).HasDefaultValueSql("datetime('now')");
        });

        modelBuilder.Entity<TraditionSubmission>(e =>
        {
            e.Property(t => t.SubmittedAt).HasDefaultValueSql("datetime('now')");
        });
    }
}
