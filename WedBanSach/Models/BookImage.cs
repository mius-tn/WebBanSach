using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class BookImage
{
    [Key]
    public int ImageID { get; set; }

    public int BookID { get; set; }

    [StringLength(255)]
    public string? ImageUrl { get; set; }

    public bool IsMain { get; set; } = false;

    // Navigation properties
    [ForeignKey("BookID")]
    public virtual Book Book { get; set; } = null!;
}
