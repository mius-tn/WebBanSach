using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models.Apriori;

public class AprioriFrequentItemset
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string ItemsetKey { get; set; } = string.Empty; // e.g. "1,5,9" - Sorted BookIDs

    public int ItemsetSize { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Support { get; set; }

    public int TransactionCount { get; set; }

    public int TrainingSessionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
