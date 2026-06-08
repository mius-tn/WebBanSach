using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WedBanSach.ViewModels;

public class ReturnRequestViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn đơn hàng.")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn sản phẩm.")]
    public int BookID { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số lượng.")]
    [Range(1, 100, ErrorMessage = "Số lượng đổi trả từ 1 đến 100 sản phẩm.")]
    public int Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Vui lòng chọn loại yêu cầu (Đổi, trả, hoàn tiền...).")]
    public string RequestType { get; set; } = string.Empty; // "Exchange", "Return", "Refund", "Warranty"

    [Required(ErrorMessage = "Vui lòng nhập lý do đổi trả.")]
    [StringLength(255, ErrorMessage = "Lý do không được vượt quá 255 ký tự.")]
    public string Reason { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả chi tiết không được vượt quá 1000 ký tự.")]
    public string? Description { get; set; }

    // Multi image uploads
    public List<IFormFile>? ProductImages { get; set; }

    public bool AcceptTerms { get; set; }
}
