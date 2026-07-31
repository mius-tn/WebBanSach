using System.ComponentModel.DataAnnotations;

namespace WedBanSach.Models.Apriori;

public class AprioriLog
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    public string Level { get; set; } = "Info"; // Info, Warning, Error

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    public string? Details { get; set; }

    public int? TrainingSessionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
