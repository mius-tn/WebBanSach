using System.ComponentModel.DataAnnotations;

namespace WedBanSach.Models;

public class Author
{
    [Key]
    public int AuthorID { get; set; }

    [Required]
    [StringLength(150)]
    public string AuthorName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    // Navigation properties
    public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}
