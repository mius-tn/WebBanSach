using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class ReturnRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }

    [Required]
    public int CustomerId { get; set; } // Points to User.UserID

    [Required]
    [StringLength(50)]
    public string RequestType { get; set; } = string.Empty; // "Exchange", "Return", "Refund", "Warranty"

    [Required]
    [StringLength(255)]
    public string Reason { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? BookID { get; set; }

    public int Quantity { get; set; } = 1;

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected", "Completed"

    [Column(TypeName = "decimal(18,2)")]
    public decimal? RefundAmount { get; set; }

    public string? AdminNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("CustomerId")]
    public virtual User Customer { get; set; } = null!;

    [ForeignKey("BookID")]
    public virtual Book? Book { get; set; }

    public virtual ICollection<ReturnRequestImage> Images { get; set; } = new List<ReturnRequestImage>();
    public virtual ICollection<RefundTransaction> RefundTransactions { get; set; } = new List<RefundTransaction>();
}
