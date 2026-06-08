using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class ReturnRequestImage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ReturnRequestId { get; set; }

    [Required]
    [StringLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [ForeignKey("ReturnRequestId")]
    public virtual ReturnRequest ReturnRequest { get; set; } = null!;
}
