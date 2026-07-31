using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class GoodsReceipt
{
    [Key]
    public int ReceiptID { get; set; }

    [StringLength(255)]
    public string? SupplierName { get; set; }

    [StringLength(100)]
    public string? EnteredBy { get; set; }

    public DateTime EntryDate { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Completed";

    [StringLength(255)]
    public string? Notes { get; set; }

    public virtual ICollection<GoodsReceiptDetail> GoodsReceiptDetails { get; set; } = new List<GoodsReceiptDetail>();
}
