using System.ComponentModel.DataAnnotations;

namespace WedBanSach.Models;

public class PaymentSetting
{
    [Key]
    public int PaymentSettingID { get; set; }

    [Required]
    [StringLength(100)]
    public string MethodName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    [StringLength(100)]
    public string? BankName { get; set; }

    [StringLength(50)]
    public string? AccountNumber { get; set; }

    [StringLength(150)]
    public string? AccountHolder { get; set; }

    [StringLength(255)]
    public string? QRCodeUrl { get; set; }

    [StringLength(20)]
    public string? BankCode { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }
}
