using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class User
{
    [Key]
    public int UserID { get; set; }

    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(20)]
    public string Status { get; set; } = "Active";

    // Verification fields
    public bool EmailVerified { get; set; } = false;
    public bool PhoneVerified { get; set; } = false;
    
    [StringLength(255)]
    public string? EmailVerificationToken { get; set; }
    
    [StringLength(10)]
    public string? PhoneVerificationCode { get; set; }
    
    public DateTime? VerificationTokenExpiry { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    // Structured Address
    [StringLength(20)]
    public string? ProvinceCode { get; set; }
    [StringLength(100)]
    public string? ProvinceName { get; set; }
    [StringLength(20)]
    public string? DistrictCode { get; set; }
    [StringLength(100)]
    public string? DistrictName { get; set; }
    [StringLength(20)]
    public string? WardCode { get; set; }
    [StringLength(100)]
    public string? WardName { get; set; }
    [StringLength(255)]
    public string? HouseNumber { get; set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();
}
