using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class Policy
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Thumbnail { get; set; }

    public bool IsPublished { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [ForeignKey("CategoryId")]
    public virtual PolicyCategory Category { get; set; } = null!;
}
