using System.ComponentModel.DataAnnotations;

namespace WedBanSach.ViewModels;

public class VerifyOtpViewModel
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mã xác thực.")]
    [Display(Name = "Mã xác thực")]
    public string OtpCode { get; set; } = string.Empty;
}
