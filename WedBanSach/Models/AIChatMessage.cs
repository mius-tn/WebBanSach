using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class AIChatMessage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SessionId { get; set; }

    [Required]
    [StringLength(50)]
    public string SenderType { get; set; } = string.Empty; // "User" or "AI"

    [Required]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("SessionId")]
    public virtual AIChatSession? AIChatSession { get; set; }
}
