using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class Promotion
{
    [Key]
    public int PromotionID { get; set; }

    [StringLength(150)]
    public string? PromotionName { get; set; }

    [StringLength(20)]
    public string? DiscountType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountValue { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}
