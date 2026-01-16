using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models;

public class UserRole
{
    public int UserID { get; set; }

    public int RoleID { get; set; }

    // Navigation properties
    [ForeignKey("UserID")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RoleID")]
    public virtual Role Role { get; set; } = null!;
}
