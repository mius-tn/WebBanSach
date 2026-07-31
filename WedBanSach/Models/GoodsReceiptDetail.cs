using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class GoodsReceiptDetail
{
    [Key]
    public int ReceiptDetailID { get; set; }

    public int ReceiptID { get; set; }
    public int BookID { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [ForeignKey("ReceiptID")]
    public virtual GoodsReceipt GoodsReceipt { get; set; } = null!;

    [ForeignKey("BookID")]
    public virtual Book Book { get; set; } = null!;
}
