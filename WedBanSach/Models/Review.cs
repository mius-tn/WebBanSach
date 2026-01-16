using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class Review
{
    [Key]
    public int ReviewID { get; set; }

    public int BookID { get; set; }

    public int UserID { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    // Navigation properties
    [ForeignKey("BookID")]
    public virtual Book Book { get; set; } = null!;

    [ForeignKey("UserID")]
    public virtual User User { get; set; } = null!;
}
