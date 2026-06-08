using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WedBanSach.Models;

public class AIChatSession
{
    [Key]
    public int Id { get; set; }

    public int? CustomerId { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.Now;

    public DateTime LastMessageAt { get; set; } = DateTime.Now;

    public virtual ICollection<AIChatMessage> AIChatMessages { get; set; } = new List<AIChatMessage>();
}
