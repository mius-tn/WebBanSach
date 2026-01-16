using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class Shipping
{
    [Key]
    public int ShippingID { get; set; }

    public int OrderID { get; set; }

    [StringLength(100)]
    public string? ShippingCompany { get; set; }

    [StringLength(100)]
    public string? TrackingNumber { get; set; }

    [StringLength(50)]
    public string? ShippingStatus { get; set; }

    // Navigation properties
    [ForeignKey("OrderID")]
    public virtual Order Order { get; set; } = null!;
}
