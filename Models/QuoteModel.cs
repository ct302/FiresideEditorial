using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FiresideEditorial.Models;

public class QuoteModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Attribution { get; set; } = string.Empty;
}
