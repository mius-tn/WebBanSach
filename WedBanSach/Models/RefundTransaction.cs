using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class RefundTransaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ReturnRequestId { get; set; }

    [Required]
    [StringLength(100)]
    public string RefundMethod { get; set; } = string.Empty; // e.g., "Bank Transfer", "COD", "Wallet"

    [Required]
    [StringLength(50)]
    public string RefundStatus { get; set; } = "Pending"; // "Pending", "Success", "Failed"

    public DateTime? RefundDate { get; set; }

    [StringLength(100)]
    public string? TransactionCode { get; set; }

    [ForeignKey("ReturnRequestId")]
    public virtual ReturnRequest ReturnRequest { get; set; } = null!;
}
