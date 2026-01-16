using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class BookCategory
{
    public int BookID { get; set; }

    public int CategoryID { get; set; }

    // Navigation properties
    [ForeignKey("BookID")]
    public virtual Book Book { get; set; } = null!;

    [ForeignKey("CategoryID")]
    public virtual Category Category { get; set; } = null!;
}
