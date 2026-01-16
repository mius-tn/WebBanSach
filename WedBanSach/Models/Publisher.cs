using System.ComponentModel.DataAnnotations;

namespace WedBanSach.Models;

public class Publisher
{
    [Key]
    public int PublisherID { get; set; }

    [Required]
    [StringLength(200)]
    public string PublisherName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Address { get; set; }

    // Navigation properties
    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}
