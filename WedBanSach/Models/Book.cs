using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class Book
{
    [Key]
    public int BookID { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [StringLength(20)]
    public string? ISBN { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal OriginalPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SalePrice { get; set; }

    public int? SalePercent { get; set; }

    public DateTime? SaleStartDate { get; set; }
    public DateTime? SaleEndDate { get; set; }
    public bool IsPromotionActive { get; set; } = false;

    public string? Description { get; set; }

    public int? PublishYear { get; set; }

    public int? Weight { get; set; } // Grams

    [StringLength(100)]
    public string? PackageSize { get; set; }

    public int? PageCount { get; set; }

    [StringLength(50)]
    public string? CoverType { get; set; }

    public int TotalStock { get; set; } = 0;
    public int ReservedStock { get; set; } = 0;
    public int SoldStock { get; set; } = 0;
    public int MinimumStock { get; set; } = 20;

    [NotMapped]
    public int AvailableStock => TotalStock - ReservedStock;

    public int FaultyQuantity { get; set; } = 0;

    public int? PublisherID { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [StringLength(20)]
    public string Status { get; set; } = "Active";

    // Navigation properties
    [ForeignKey("PublisherID")]
    public virtual Publisher? Publisher { get; set; }

    public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    public virtual ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
    public virtual ICollection<BookImage> BookImages { get; set; } = new List<BookImage>();
    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}
