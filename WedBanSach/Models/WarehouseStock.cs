using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class WarehouseStock
{
    public int WarehouseID { get; set; }
    public int BookID { get; set; }

    public int TotalStock { get; set; } = 0;
    public int ReservedStock { get; set; } = 0;

    [NotMapped]
    public int AvailableStock => TotalStock - ReservedStock;

    [ForeignKey("WarehouseID")]
    public virtual Warehouse Warehouse { get; set; } = null!;

    [ForeignKey("BookID")]
    public virtual Book Book { get; set; } = null!;
}
