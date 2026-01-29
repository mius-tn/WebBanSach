using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class Category
{
    [Key]
    public int CategoryID { get; set; }

    [Required]
    [StringLength(150)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Slug { get; set; }

    public int? ParentCategoryID { get; set; }

    // Navigation properties
    [ForeignKey("ParentCategoryID")]
    public virtual Category? ParentCategory { get; set; }

    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public virtual ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
}
