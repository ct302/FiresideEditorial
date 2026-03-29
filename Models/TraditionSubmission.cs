using System.ComponentModel.DataAnnotations;

namespace FiresideEditorial.Models;

public class TraditionSubmission
{
    [Required(ErrorMessage = "Please share your name.")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tell us about your tradition!")]
    [MaxLength(2000)]
    public string Story { get; set; } = string.Empty;
}
