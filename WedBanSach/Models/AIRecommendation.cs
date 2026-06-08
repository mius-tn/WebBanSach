using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class AIRecommendation
{
    [Key]
    public int Id { get; set; }

    public int? CustomerId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal RecommendationScore { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("ProductId")]
    public virtual Book? Product { get; set; }
}
