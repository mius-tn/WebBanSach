using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Data;
using WedBanSach.Helpers;
using WedBanSach.Models;

namespace WedBanSach.Controllers;

[AuthorizeAdmin]
public class AdminUsersController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminUsersController(BookStoreDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string searchTerm = "", int page = 1, int pageSize = 20)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u => u.FullName.Contains(searchTerm) || 
                                   u.Email.Contains(searchTerm) ||
                                   (u.Phone != null && u.Phone.Contains(searchTerm)));
        }

        var totalRecords = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound();

        user.Status = user.Status == "Active" ? "Inactive" : "Active";
        await _context.SaveChangesAsync();

        return Json(new { success = true, status = user.Status });
    }

    [HttpGet]
    public async Task<IActionResult> EditRoles(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.UserID == id);

        if (user == null)
            return NotFound();

        var allRoles = await _context.Roles.ToListAsync();
        var userRoleIds = user.UserRoles.Select(ur => ur.RoleID).ToList();

        ViewBag.AllRoles = allRoles;
        ViewBag.UserRoleIds = userRoleIds;
        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRoles(int id, List<int> roleIds)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.UserID == id);

        if (user == null)
            return NotFound();

        // Remove existing roles
        var existingRoles = user.UserRoles.ToList();
        _context.UserRoles.RemoveRange(existingRoles);

        // Add new roles
        if (roleIds != null && roleIds.Any())
        {
            foreach (var roleId in roleIds)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserID = id,
                    RoleID = roleId
                });
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật quyền thành công!";
        return RedirectToAction(nameof(Index));
    }
}
