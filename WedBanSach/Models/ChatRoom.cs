using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WedBanSach.Models
{
    public class ChatRoom
    {
        [Key]
        public int RoomID { get; set; }

        public int? UserID { get; set; } // Nullable if guest chat is allowed, but requirements say User/Admin

        // Assuming UserID links to Users table. 
        // Admin might be a User with a specific Role, or a separate table. 
        // Based on typical simple setups, Admin is often a User. 
        // Let's keep it simple for now and just store IDs.

        public int? AdminID { get; set; } // The admin currently handling the chat

        public string LastMessage { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties if needed later
        // [ForeignKey("UserID")]
        // public virtual User User { get; set; }
    }
}
