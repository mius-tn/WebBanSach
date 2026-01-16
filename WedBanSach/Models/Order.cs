using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class Order
{
    [Key]
    public int OrderID { get; set; }

    public int UserID { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TotalAmount { get; set; }

    [StringLength(50)]
    public string? OrderStatus { get; set; }

    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    [StringLength(255)]
    public string? ShippingAddress { get; set; }

    [StringLength(100)]
    public string? ShippingMethodName { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ShippingFee { get; set; }

    // Navigation properties
    [ForeignKey("UserID")]
    public virtual User User { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<Shipping> Shippings { get; set; } = new List<Shipping>();
}
