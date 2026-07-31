using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models.Apriori;

public class AprioriTrainingHistory
{
    [Key]
    public int Id { get; set; }

    public DateTime StartTime { get; set; } = DateTime.Now;

    public DateTime? EndTime { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Running"; // Running, Completed, Failed, Cancelled

    public int TotalTransactions { get; set; }

    public int TotalItems { get; set; }

    public int TotalFrequentItemsets { get; set; }

    public int TotalRules { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal MinSupportUsed { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal MinConfidenceUsed { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal MinLiftUsed { get; set; }

    public long DurationMs { get; set; }

    public string? ErrorMessage { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; } // System or UserId
}
