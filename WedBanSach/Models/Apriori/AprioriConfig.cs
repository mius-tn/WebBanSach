using System.ComponentModel.DataAnnotations;

namespace WedBanSach.Models.Apriori;

public class AprioriConfig
{
    [Key]
    public int Id { get; set; }

    [Required]
    public double MinSupport { get; set; } = 0.01; // 1%

    [Required]
    public double MinConfidence { get; set; } = 0.5; // 50%

    [Required]
    public double MinLift { get; set; } = 1.0;

    [Required]
    public int MaxItemsetSize { get; set; } = 5;

    [Required]
    public int MinTransactionCount { get; set; } = 50;

    [Required]
    public bool AutoRetrain { get; set; } = true;

    [Required]
    public int TrainingIntervalHours { get; set; } = 24;

    [Required]
    public int CacheTimeMinutes { get; set; } = 60;
}
