using Microsoft.AspNetCore.SignalR;
using WedBanSach.Data;
using WedBanSach.Models;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace WedBanSach.Hubs
{
    public class ChatHub : Hub
    {
        private readonly BookStoreDbContext _context;

        public ChatHub(BookStoreDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(string userIdentifier, string message, string type = "Text")
        {
            // Try to find user by Email or FullName (Case Insensitive)
            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == userIdentifier.ToLower().Trim() || u.FullName.ToLower() == userIdentifier.ToLower().Trim());
            if (user == null) return;

            var room = _context.ChatRooms.FirstOrDefault(r => r.UserID == user.UserID);
            if (room == null)
            {
                room = new ChatRoom
                {
                    UserID = user.UserID,
                    LastMessage = type == "Image" ? "[Hình ảnh]" : message,
                    UpdatedAt = DateTime.Now
                };
                _context.ChatRooms.Add(room);
                await _context.SaveChangesAsync();
            }
            else
            {
                room.LastMessage = type == "Image" ? "[Hình ảnh]" : message;
                room.UpdatedAt = DateTime.Now;
                _context.ChatRooms.Update(room);
            }

            // Save Message
            var chatMessage = new ChatMessage
            {
                RoomID = room.RoomID,
                SenderRole = "User",
                SenderID = user.UserID,
                MessageContent = message,
                MessageType = type,
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Ensure User is in the Group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{room.RoomID}");

            // DEBUG: Broadcast to ALL to rule out Group issues
            Console.WriteLine($"[ChatHub] Sending message from {user.FullName} (ID: {user.UserID}) to Admins. RoomID: {room.RoomID}");
            await Clients.All.SendAsync("ReceiveUserMessage", user.UserID, user.FullName ?? "Khách", user.AvatarUrl, message, room.RoomID, type, user.Email);
            
            // Confirm to User
            await Clients.Caller.SendAsync("ReceiveMessageConfirmation", message, type);
        }

        // Fetch history for the calling user
        public async Task GetClientHistory(string userIdentifier)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == userIdentifier || u.FullName == userIdentifier);
            if (user == null) return;

            var room = _context.ChatRooms.FirstOrDefault(r => r.UserID == user.UserID);
            if (room != null)
            {
                if (room != null)
                {
                    var dbMessages = _context.ChatMessages
                       .Where(m => m.RoomID == room.RoomID)
                       .OrderBy(m => m.CreatedAt)
                       .ToList();

                    var messages = dbMessages.Select(m => new
                    {
                        role = m.SenderRole == "Admin" ? "admin" : "user",
                        content = m.MessageContent,
                        type = m.MessageType ?? "Text"
                    })
                       .ToList();

                    await Clients.Caller.SendAsync("ReceiveHistory", messages);

                    // Also ensure they are in the group
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{room.RoomID}");
                }
            }
        }

        public async Task AdminReply(int roomId, string message, string type = "Text")
        {
            var room = await _context.ChatRooms.FindAsync(roomId);
            if (room == null) return;

            room.LastMessage = type == "Image" ? "[Hình ảnh]" : message;
            room.UpdatedAt = DateTime.Now;
            _context.Update(room);

            var chatMessage = new ChatMessage
            {
                RoomID = roomId,
                SenderRole = "Admin",
                SenderID = 0, 
                MessageContent = message,
                MessageType = type,
                IsRead = true,
                CreatedAt = DateTime.Now
            };
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Broadcast to Room (User + Admin listening) - Exclude caller to avoid duplicate if client appends locally
            // But we need to be careful: Admin might have multiple tabs? 
            // For now, assume single session per admin for simplicity or just duplicate check in JS.
            // Using GroupExcept is cleaner for Optimistic UI on sender side.
            await Clients.GroupExcept($"Room_{roomId}", BuildList(Context.ConnectionId)).SendAsync("ReceiveAdminReply", message, type);
        }

        private IReadOnlyList<string> BuildList(string item)
        {
            return new List<string> { item };
        }

        public async Task JoinRoom(int roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{roomId}");
        }

        public async Task JoinAdminGroup()
        {
             await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }

        // New method for User to rejoin their room on refresh
        public async Task JoinOwnRoom(string userEmail)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            if (user == null) return;

            var room = _context.ChatRooms.FirstOrDefault(r => r.UserID == user.UserID);
            if (room != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{room.RoomID}");
            }
        }

        public async Task DeleteRoom(int roomId)
        {
            var room = await _context.ChatRooms.FindAsync(roomId);
            if (room != null)
            {
                // Delete messages
                var messages = _context.ChatMessages.Where(m => m.RoomID == roomId);
                _context.ChatMessages.RemoveRange(messages);
                
                // Delete room
                _context.ChatRooms.Remove(room);
                await _context.SaveChangesAsync();

                // Notify Admin to remove from list
                await Clients.Group("Admins").SendAsync("ReceiveDeleteRoom", roomId);

                // Notify User to reset chat
                await Clients.Group($"Room_{roomId}").SendAsync("ReceiveChatReset");
            }
        }
    }
}
