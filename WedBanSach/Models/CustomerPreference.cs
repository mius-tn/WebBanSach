using System.ComponentModel.DataAnnotations;

namespace WedBanSach.Models;

public class CustomerPreference
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [StringLength(500)]
    public string? FavoriteGenres { get; set; }

    [StringLength(500)]
    public string? FavoriteAuthors { get; set; }

    [StringLength(100)]
    public string? PreferredPriceRange { get; set; }
}
