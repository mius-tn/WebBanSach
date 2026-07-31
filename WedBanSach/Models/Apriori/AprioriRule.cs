using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models.Apriori;

public class AprioriRule
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string AntecedentKey { get; set; } = string.Empty; // BookIDs, e.g., "1,2"

    [Required]
    [StringLength(255)]
    public string ConsequentKey { get; set; } = string.Empty; // BookIDs, e.g., "3"

    [Column(TypeName = "decimal(18,4)")]
    public decimal Support { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Confidence { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Lift { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Conviction { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Leverage { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal JaccardSimilarity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CosineSimilarity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Kulczynski { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AllConfidence { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal MaxConfidence { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal RecommendationScore { get; set; }

    public int TrainingSessionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;
}
