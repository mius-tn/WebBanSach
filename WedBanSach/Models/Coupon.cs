using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class Coupon
{
    [Key]
    public int CouponID { get; set; }

    [StringLength(50)]
    public string? Code { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountValue { get; set; }

    public DateTime? ExpiredDate { get; set; }

    public int? UsageLimit { get; set; }
}
