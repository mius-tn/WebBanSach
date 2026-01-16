using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class OrderDetail
{
    [Key]
    public int OrderDetailID { get; set; }

    public int OrderID { get; set; }

    public int BookID { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? UnitPrice { get; set; }

    // Navigation properties
    [ForeignKey("OrderID")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("BookID")]
    public virtual Book Book { get; set; } = null!;
}
