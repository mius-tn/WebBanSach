using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Helpers;

public static class AuthHelper
{
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    public static bool VerifyPassword(string password, string hash)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hash;
    }

    public static async Task<User?> GetCurrentUserAsync(HttpContext httpContext, BookStoreDbContext dbContext)
    {
        var userIdStr = httpContext.Session.GetString("UserId");
        if (int.TryParse(userIdStr, out var userId))
        {
            return await dbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserID == userId && u.Status == "Active");
        }
        return null;
    }

    public static bool IsAdmin(User? user)
    {
        return user?.UserRoles?.Any(ur => ur.Role.RoleName == "Admin") ?? false;
    }

    public static bool IsStaff(User? user)
    {
        return user?.UserRoles?.Any(ur => ur.Role.RoleName == "Staff" || ur.Role.RoleName == "Admin") ?? false;
    }
}
