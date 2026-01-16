using System;
using System.Collections.Generic;
using WedBanSach.Models;

namespace WedBanSach.ViewModels
{
    public class AdminChatViewModel
    {
        public List<ChatRoom> Rooms { get; set; }
        public int SelectedRoomId { get; set; }
        public List<ChatMessage> CurrentMessages { get; set; }
        public User SelectedUser { get; set; }
        public Dictionary<int, User> Users { get; set; } // Map UserID to User
    }
}
