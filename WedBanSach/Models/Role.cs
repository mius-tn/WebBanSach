using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace WedBanSach.Models;

public class Role
{
    [Key]
    public int RoleID { get; set; }

    [Required]
    [StringLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Active";

    // Stores permissions as a JSON string array, e.g., ["User.View", "Product.Create"]
    public string Permissions { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    [NotMapped]
    public List<string> PermissionList
    {
        get => string.IsNullOrEmpty(Permissions) 
            ? new List<string>() 
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(Permissions) ?? new List<string>();
        set => Permissions = System.Text.Json.JsonSerializer.Serialize(value);
    }
}
