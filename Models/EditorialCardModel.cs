using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FiresideEditorial.Models;

public class EditorialCardModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ImageAlt { get; set; } = string.Empty;
    public string CtaText { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    [NotMapped]
    public string ArticleBody { get; set; } = string.Empty;
    public string AffiliateUrl { get; set; } = string.Empty;
    public string AffiliateLabel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
