using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models
{
    public class ChatMessage
    {
        [Key]
        public int MessageID { get; set; }

        public int RoomID { get; set; }

        [Required]
        public string SenderRole { get; set; } // "User" or "Admin"

        public int SenderID { get; set; }

        [Required]
        public string MessageContent { get; set; }

        public string? MessageType { get; set; } = "Text"; // "Text" or "Image"

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("RoomID")]
        public virtual ChatRoom ChatRoom { get; set; }
    }
}
