using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class Payment
{
    [Key]
    public int PaymentID { get; set; }

    public int OrderID { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Amount { get; set; }

    [StringLength(50)]
    public string? PaymentStatus { get; set; }

    [StringLength(100)]
    public string? TransactionCode { get; set; }

    // Navigation properties
    [ForeignKey("OrderID")]
    public virtual Order Order { get; set; } = null!;
}
