using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class BookAuthor
{
    public int BookID { get; set; }

    public int AuthorID { get; set; }

    // Navigation properties
    [ForeignKey("BookID")]
    public virtual Book Book { get; set; } = null!;

    [ForeignKey("AuthorID")]
    public virtual Author Author { get; set; } = null!;
}
