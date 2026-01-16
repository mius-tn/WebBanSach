using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class CartItem
{
    [Key]
    public int CartItemID { get; set; }

    public int CartID { get; set; }

    public int BookID { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    // Navigation properties
    [ForeignKey("CartID")]
    public virtual Cart Cart { get; set; } = null!;

    [ForeignKey("BookID")]
    public virtual Book Book { get; set; } = null!;
}
