using System.ComponentModel.DataAnnotations;

namespace WedBanSach.Models;

public class AuditLog
{
    [Key]
    public int AuditLogID { get; set; }

    [StringLength(100)]
    public string Action { get; set; } = string.Empty; // "Create", "Update", "Delete"

    [StringLength(100)]
    public string EntityName { get; set; } = string.Empty;

    public string? RecordID { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    [StringLength(100)]
    public string? UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
