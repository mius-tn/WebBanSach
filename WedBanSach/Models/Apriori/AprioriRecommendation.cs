using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models.Apriori;

public class AprioriRecommendation
{
    [Key]
    public int Id { get; set; }

    public int SourceBookId { get; set; }

    public int RecommendedBookId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Score { get; set; }

    public int? RuleId { get; set; } // FK to AprioriRule

    [StringLength(50)]
    public string RecommendationType { get; set; } = "FrequentlyBoughtTogether"; // CrossSell, Upsell

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? ExpiresAt { get; set; }
}
