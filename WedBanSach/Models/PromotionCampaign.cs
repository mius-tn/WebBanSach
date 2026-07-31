using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class PromotionCampaign
{
    [Key]
    public int CampaignID { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    [StringLength(50)]
    public string DiscountType { get; set; } = "Percentage"; // "Percentage" or "FixedAmount"

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountValue { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    // Navigation properties
    public virtual ICollection<CampaignBook> CampaignBooks { get; set; } = new List<CampaignBook>();
}
