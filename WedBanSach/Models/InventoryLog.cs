using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class InventoryLog
{
    [Key]
    public int LogID { get; set; }

    public int BookID { get; set; }

    public int ChangeQuantity { get; set; }

    [StringLength(255)]
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey("BookID")]
    public virtual Book Book { get; set; } = null!;
}
