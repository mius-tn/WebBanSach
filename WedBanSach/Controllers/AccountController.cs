using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Helpers;
using WedBanSach.Models;
using WedBanSach.ViewModels;

namespace WedBanSach.Controllers;

public class AccountController : Controller
{
    private readonly BookStoreDbContext _context;
    private readonly WedBanSach.Services.EmailService _emailService;

    public AccountController(BookStoreDbContext context, WedBanSach.Services.EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        // If already logged in, redirect based on role
        if (HttpContext.Session.GetString("UserId") != null)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            var isAdmin = HttpContext.Session.GetString("IsAdmin");
            if (isAdmin == "True")
            {
                return RedirectToAction("Index", "Admin");
            }
            return RedirectToAction("Index", "Home");
        }
        return View(new WedBanSach.ViewModels.RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.Status == "Active");

        if (user != null && AuthHelper.VerifyPassword(password, user.PasswordHash))
        {
            // Set session for all users
            HttpContext.Session.SetString("UserId", user.UserID.ToString());
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserAvatar", user.AvatarUrl ?? "");
            HttpContext.Session.SetString("IsAdmin", AuthHelper.IsAdmin(user).ToString());
            
            // Redirect based on returnUrl first
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Redirect based on role
            if (AuthHelper.IsStaff(user))
            {
                // Admin/Staff → Admin Panel
                return RedirectToAction("Index", "Admin");
            }
            else
            {
                // Customer → Homepage with success message
                TempData["LoginSuccess"] = $"Chào mừng {user.FullName}!";
                return RedirectToAction("Index", "Home");
            }
        }
        else
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
        }

        return View();
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Display registration form
    /// </summary>
    [HttpGet]
    public IActionResult Register()
    {
        // If already logged in, redirect to homepage
        if (HttpContext.Session.GetString("UserId") != null)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    /// <summary>
    /// Process customer registration
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.IsRegisterMode = true;
            return View("Login", model);
        }

        // Verify Email OTP
        var sessionOtp = HttpContext.Session.GetString("EmailOtp");
        var otpEmail = HttpContext.Session.GetString("OtpEmail");
        var expiry = HttpContext.Session.GetInt32("OtpExpiry");

        if (string.IsNullOrEmpty(sessionOtp) || otpEmail != model.Email || expiry < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            ModelState.AddModelError("", "Mã xác thực đã hết hạn hoặc không hợp lệ. Vui lòng gửi lại.");
            ViewBag.IsRegisterMode = true;
            return View("Login", model);
        }

        if (sessionOtp != model.EmailOtpCode)
        {
            ModelState.AddModelError("", "Mã xác thực không chính xác.");
            ViewBag.IsRegisterMode = true;
            return View("Login", model);
        }

        // Check if email already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "Email này đã được sử dụng.");
            ViewBag.IsRegisterMode = true;
            return View("Login", model);
        }

        try
        {
            // Create new user
            var newUser = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                PasswordHash = AuthHelper.HashPassword(model.Password),
                Status = "Active",
                CreatedAt = DateTime.Now,
                EmailVerified = true // Mark as verified since OTP was correct
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Assign Customer role
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
            if (customerRole != null)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserID = newUser.UserID,
                    RoleID = customerRole.RoleID
                });
                await _context.SaveChangesAsync();
            }

            // Redirect to login page with success message
            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại.");
            ViewBag.IsRegisterMode = true;
            return View("Login", model);
        }
    }

    /// <summary>
    /// Hiển thị trang khởi tạo Admin
    /// </summary>
    [HttpGet]
    public IActionResult InitializeAdmin()
    {
        return View();
    }

    /// <summary>
    /// API endpoint để khởi tạo tài khoản Admin mặc định
    /// Email: admin@bookstore.com
    /// Password: Admin@123
    /// Chỉ nên chạy một lần khi setup hệ thống
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SendEmailOtp(string email)
    {
        if (string.IsNullOrEmpty(email))
            return Json(new { success = false, message = "Email không được để trống." });

        if (await _context.Users.AnyAsync(u => u.Email == email))
            return Json(new { success = false, message = "Email này đã được đăng ký." });

        var otp = new Random().Next(100000, 999999).ToString();
        
        HttpContext.Session.SetString("EmailOtp", otp);
        HttpContext.Session.SetString("OtpEmail", email);
        HttpContext.Session.SetInt32("OtpExpiry", (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 300);

        var result = await _emailService.SendPhoneOTPEmailAsync(email, "Quý khách", otp);

        if (result)
            return Json(new { success = true, message = "Mã xác thực đã được gửi đến email của bạn." });
        
        return Json(new { success = false, message = "Không thể gửi email. Vui lòng kiểm tra cấu hình SMTP." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("InitializeAdmin")]
    public async Task<IActionResult> InitializeAdminPost()
    {
        try
        {
            // Kiểm tra xem đã có Admin role chưa
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (adminRole == null)
            {
                adminRole = new Role { RoleName = "Admin" };
                _context.Roles.Add(adminRole);
                await _context.SaveChangesAsync();
            }

            // Kiểm tra xem đã có Staff role chưa
            var staffRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Staff");
            if (staffRole == null)
            {
                staffRole = new Role { RoleName = "Staff" };
                _context.Roles.Add(staffRole);
                await _context.SaveChangesAsync();
            }

            // Kiểm tra xem đã có Customer role chưa
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
            if (customerRole == null)
            {
                customerRole = new Role { RoleName = "Customer" };
                _context.Roles.Add(customerRole);
                await _context.SaveChangesAsync();
            }

            // Kiểm tra xem đã có admin user chưa
            var existingAdmin = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == "admin@bookstore.com");

            if (existingAdmin == null)
            {
                // Tạo admin user mặc định
                var adminUser = new User
                {
                    FullName = "Administrator",
                    Email = "admin@bookstore.com",
                    Phone = "0123456789",
                    PasswordHash = AuthHelper.HashPassword("Admin@123"), // Mật khẩu mặc định
                    Status = "Active",
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                // Gán quyền Admin
                _context.UserRoles.Add(new UserRole
                {
                    UserID = adminUser.UserID,
                    RoleID = adminRole.RoleID
                });

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đã tạo tài khoản Admin mặc định thành công!",
                    email = "admin@bookstore.com",
                    password = "Admin@123",
                    note = "Vui lòng đổi mật khẩu sau khi đăng nhập!"
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "Tài khoản Admin đã tồn tại!",
                    email = existingAdmin.Email
                });
            }
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = "Lỗi khi khởi tạo: " + ex.Message
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Notifications(int page = 1)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

        int userId = int.Parse(userIdStr);
        int pageSize = 12;

        var query = _context.Notifications
            .Where(n => n.UserID == userId)
            .OrderByDescending(n => n.CreatedAt);

        var totalItems = await query.CountAsync();
        var notifications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var displayList = new List<NotificationDisplayDto>();

        foreach (var noti in notifications)
        {
            var dto = new NotificationDisplayDto { Notification = noti };

            // Try to extract Order Info if it's an Order notification
            if (noti.Type == "Order" && !string.IsNullOrEmpty(noti.RedirectUrl))
            {
                // Expected format: /Order/Details/123
                var parts = noti.RedirectUrl.Split('/');
                if (parts.Length > 0 && int.TryParse(parts.Last(), out int orderId))
                {
                    var order = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Book)
                        .ThenInclude(b => b.BookImages)
                        .FirstOrDefaultAsync(o => o.OrderID == orderId);

                    if (order != null)
                    {
                        var firstItem = order.OrderDetails.FirstOrDefault();
                        dto.OrderInfo = new OrderNotificationInfo
                        {
                            OrderId = orderId,
                            OrderStatus = order.OrderStatus ?? "Đang xử lý",
                            TotalAmount = order.TotalAmount ?? 0,
                            ProductName = firstItem?.Book.Title ?? "Sản phẩm",
                            ProductImage = firstItem?.Book.BookImages.FirstOrDefault()?.ImageUrl ?? "/images/default-book.png"
                        };
                    }
                }
            }
            displayList.Add(dto);
        }

        var viewModel = new NotificationListViewModel
        {
            Notifications = displayList,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr)) return Json(new { success = false });

        int userId = int.Parse(userIdStr);
        var noti = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationID == id && n.UserID == userId);

        if (noti != null)
        {
            noti.IsRead = true;
            await _context.SaveChangesAsync();

            var unreadCount = await _context.Notifications.CountAsync(n => n.UserID == userId && !n.IsRead);
            return Json(new { success = true, unreadCount });
        }

        return Json(new { success = false });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr)) return Json(new { success = false });

        int userId = int.Parse(userIdStr);
        var unreadNotis = await _context.Notifications.Where(n => n.UserID == userId && !n.IsRead).ToListAsync();

        foreach (var noti in unreadNotis)
        {
            noti.IsRead = true;
        }

        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpGet]
    [Route("Account/Notifications/Detail/{id}")]
    public async Task<IActionResult> NotificationDetails(int id)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

        int userId = int.Parse(userIdStr);
        
        var noti = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationID == id && n.UserID == userId);

        if (noti == null) return NotFound();

        // Mark as read if opened
        if (!noti.IsRead)
        {
            noti.IsRead = true;
            await _context.SaveChangesAsync();
        }

        var viewModel = new NotificationDetailViewModel
        {
            Notification = noti
        };

        // Try to fetch order details if applicable
        // Try to fetch order details if applicable
        int orderId = 0;
        bool foundOrderId = false;

        // 1. Try to extract from RedirectUrl
        if (!string.IsNullOrEmpty(noti.RedirectUrl))
        {
             var parts = noti.RedirectUrl.Split('/');
             if (parts.Length > 0 && int.TryParse(parts.Last(), out orderId))
             {
                 foundOrderId = true;
             }
        }

        // 2. Fallback: Try to extract from Message (e.g. "Đơn hàng #35...")
        if (!foundOrderId && !string.IsNullOrEmpty(noti.Message))
        {
            var match = System.Text.RegularExpressions.Regex.Match(noti.Message, @"#(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out orderId))
            {
                foundOrderId = true;
            }
        }

        if (foundOrderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
                    .ThenInclude(b => b.BookImages)
                .Include(o => o.Payments)
                .Include(o => o.Shippings)
                .Include(o => o.User) // Include User for Receiver Info
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            viewModel.Order = order;
        }

        return View(viewModel);
    }
    // --- Profile & Address Management ---

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");
        int userId = int.Parse(userIdStr);
        var user = await _context.Users.FindAsync(userId);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(string fullName, string phone, string? password, string? newPassword, IFormFile? avatarFile)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");
        int userId = int.Parse(userIdStr);
        var user = await _context.Users.FindAsync(userId);
        
        if (user != null)
        {
            user.FullName = fullName;
            user.Phone = phone;

            if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(newPassword))
            {
                if (AuthHelper.VerifyPassword(password, user.PasswordHash))
                {
                    user.PasswordHash = AuthHelper.HashPassword(newPassword);
                }
                else
                {
                    TempData["Error"] = "Mật khẩu hiện tại không đúng.";
                    return RedirectToAction("Profile");
                }
            }

            // Handle Avatar Upload
            if (avatarFile != null && avatarFile.Length > 0)
            {
                var fileName = $"avatar_{user.UserID}_{DateTime.Now.Ticks}{Path.GetExtension(avatarFile.FileName)}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/avatars", fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                user.AvatarUrl = "/images/avatars/" + fileName;
                HttpContext.Session.SetString("UserAvatar", user.AvatarUrl);
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.SetString("UserName", user.FullName); // Update session name
            TempData["Success"] = "Cập nhật thông tin thành công!";
        }
        return RedirectToAction("Profile");
    }

    [HttpGet]
    public async Task<IActionResult> Addresses()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");
        
        int userId = int.Parse(userIdStr);
        var addresses = await _context.UserAddresses
            .Where(a => a.UserID == userId)
            .OrderByDescending(a => a.IsDefault)
            .ToListAsync();
            
        return View(addresses);
    }

    [HttpPost]
    public async Task<IActionResult> SaveAddress(UserAddress model)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");
        int userId = int.Parse(userIdStr);

        model.UserID = userId; // Force UserID

        // Auto-populate Receiver Info from User Profile (since inputs were removed)
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            model.ReceiverName = user.FullName;
            model.Phone = user.Phone;
        }

        if (model.IsDefault)
        {
            // Unset other defaults
            var others = _context.UserAddresses.Where(a => a.UserID == userId && a.IsDefault);
            foreach (var a in others) a.IsDefault = false;
        }
        else if (!await _context.UserAddresses.AnyAsync(a => a.UserID == userId))
        {
             // First address is always default
             model.IsDefault = true;
        }

        if (model.AddressID > 0)
        {
            // Update
            var existing = await _context.UserAddresses.FindAsync(model.AddressID);
            if (existing != null && existing.UserID == userId)
            {
                existing.ReceiverName = model.ReceiverName;
                existing.Phone = model.Phone;
                existing.AddressDetail = model.AddressDetail;
                existing.ProvinceCode = model.ProvinceCode;
                existing.ProvinceName = model.ProvinceName;
                existing.DistrictCode = model.DistrictCode;
                existing.DistrictName = model.DistrictName;
                existing.WardCode = model.WardCode;
                existing.WardName = model.WardName;
                existing.IsDefault = model.IsDefault;
            }
        }
        else
        {
            // Add New
            _context.UserAddresses.Add(model);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Addresses");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr)) return Json(new { success = false });

        int userId = int.Parse(userIdStr);
        var address = await _context.UserAddresses.FirstOrDefaultAsync(a => a.AddressID == id && a.UserID == userId);

        if (address != null)
        {
            _context.UserAddresses.Remove(address);
            await _context.SaveChangesAsync();
        }
        return Json(new { success = true });
    }
}

