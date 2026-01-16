using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models;
using WedBanSach.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace WedBanSach.Controllers
{
    // [Authorize(Roles = "Admin")] // Uncomment if Auth is set up
    public class AdminChatController : Controller
    {
        private readonly BookStoreDbContext _context;

        public AdminChatController(BookStoreDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? roomId)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "True")
            {
                return RedirectToAction("Index", "Home");
            }

            var rooms = await _context.ChatRooms
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();

            // Fetch User details
            var userIds = rooms.Where(r => r.UserID.HasValue).Select(r => r.UserID.Value).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserID))
                .ToDictionaryAsync(u => u.UserID);

            var viewModel = new AdminChatViewModel
            {
                Rooms = rooms,
                CurrentMessages = new List<ChatMessage>(),
                Users = users
            };

            if (roomId.HasValue)
            {
                viewModel.SelectedRoomId = roomId.Value;
                viewModel.CurrentMessages = await _context.ChatMessages
                    .Where(m => m.RoomID == roomId.Value)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();
                
                var room = rooms.FirstOrDefault(r => r.RoomID == roomId.Value);
                if (room != null && room.UserID.HasValue)
                {
                    viewModel.SelectedUser = await _context.Users.FindAsync(room.UserID.Value);
                }
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int roomId)
        {
            var messages = await _context.ChatMessages
                .Where(m => m.RoomID == roomId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
            
            return Json(messages);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetUserInfo(int userId)
        {
             var user = await _context.Users.FindAsync(userId);
             // Return partial view or json
             return Json(new { fullName = user.FullName, email = user.Email, phone = user.Phone }); // Adjust fields
        }
    }
}
