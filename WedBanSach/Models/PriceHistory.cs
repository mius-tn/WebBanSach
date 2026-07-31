using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class PriceHistory
{
    [Key]
    public int PriceHistoryID { get; set; }

    public int BookID { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OldPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NewPrice { get; set; }

    [StringLength(50)]
    public string ChangeType { get; set; } = string.Empty; // e.g., "Manual", "PromotionStart", "PromotionEnd"

    [StringLength(100)]
    public string? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.Now;

    [StringLength(255)]
    public string? Reason { get; set; }

    [ForeignKey("BookID")]
    public virtual Book Book { get; set; } = null!;
}
