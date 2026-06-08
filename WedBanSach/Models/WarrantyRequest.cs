using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class WarrantyRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; } // Points to Book.BookID

    [Required]
    public int CustomerId { get; set; } // Points to User.UserID

    [Required]
    public string IssueDescription { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected", "Completed"

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("ProductId")]
    public virtual Book Product { get; set; } = null!;

    [ForeignKey("CustomerId")]
    public virtual User Customer { get; set; } = null!;
}
