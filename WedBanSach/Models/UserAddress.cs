using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class UserAddress
{
    [Key]
    public int AddressID { get; set; }

    public int UserID { get; set; }

    [Required]
    [StringLength(150)]
    public string ReceiverName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string AddressDetail { get; set; } = string.Empty; // House number, street

    [StringLength(20)]
    public string? ProvinceCode { get; set; }
    [StringLength(100)]
    public string? ProvinceName { get; set; }
    
    [StringLength(20)]
    public string? DistrictCode { get; set; }
    [StringLength(100)]
    public string? DistrictName { get; set; }
    
    [StringLength(20)]
    public string? WardCode { get; set; }
    [StringLength(100)]
    public string? WardName { get; set; }

    public bool IsDefault { get; set; } = false;

    // Navigation
    [ForeignKey("UserID")]
    public virtual User? User { get; set; }
}
