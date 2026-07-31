using System.ComponentModel.DataAnnotations;

namespace WedBanSach.Models;

public class Warehouse
{
    [Key]
    public int WarehouseID { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? ContactPhone { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Active";

    public virtual ICollection<WarehouseStock> WarehouseStocks { get; set; } = new List<WarehouseStock>();
}
