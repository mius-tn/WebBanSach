using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Attributes;
using WedBanSach.Constants;
using WedBanSach.Data;
using WedBanSach.Models;

namespace WedBanSach.Controllers;

[Route("Admin/Roles")] // Use explicit route if needed, or rely on Area/Controller convention
// Assuming we want this protected by Admin + Permission
// [Permission(SystemPermissions.Module_Role, SystemPermissions.Action_View)] // Apply on methods
public class AdminRolesController : Controller
{
    private readonly BookStoreDbContext _context;

    public AdminRolesController(BookStoreDbContext context)
    {
        _context = context;
    }

    // GET: Admin/Roles
    [HttpGet]
    [Permission(SystemPermissions.Module_Role, SystemPermissions.Action_View)]
    public async Task<IActionResult> Index()
    {
        var roles = await _context.Roles.ToListAsync();
        return View(roles);
    }

    // GET: Admin/Roles/Create
    [HttpGet]
    [Route("Create")]
    [Permission(SystemPermissions.Module_Role, SystemPermissions.Action_Create)]
    public IActionResult Create()
    {
        ViewBag.AllPermissions = SystemPermissions.GetAllPermissions();
        return View(new Role());
    }

    // POST: Admin/Roles/Create
    [HttpPost]
    [Route("Create")]
    [Permission(SystemPermissions.Module_Role, SystemPermissions.Action_Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Role role, List<string> selectedPermissions)
    {
        if (ModelState.IsValid)
        {
            // Prevent duplicate role name
            if (await _context.Roles.AnyAsync(r => r.RoleName == role.RoleName))
            {
                ModelState.AddModelError("RoleName", "Tên vai trò đã tồn tại.");
                ViewBag.AllPermissions = SystemPermissions.GetAllPermissions();
                return View(role);
            }

            role.PermissionList = selectedPermissions ?? new List<string>();
            role.CreatedAt = DateTime.Now;
            
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.AllPermissions = SystemPermissions.GetAllPermissions();
        return View(role);
    }

    // GET: Admin/Roles/Edit/5
    [HttpGet]
    [Route("Edit/{id}")]
    [Permission(SystemPermissions.Module_Role, SystemPermissions.Action_Update)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var role = await _context.Roles.FindAsync(id);
        if (role == null) return NotFound();

        // Prevent editing Super Admin permissions if strict
        // if (role.RoleName == "Super Admin") ...

        ViewBag.AllPermissions = SystemPermissions.GetAllPermissions();
        return View(role);
    }

    // POST: Admin/Roles/Edit/5
    [HttpPost]
    [Route("Edit/{id}")]
    [Permission(SystemPermissions.Module_Role, SystemPermissions.Action_Update)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Role role, List<string> selectedPermissions)
    {
        if (id != role.RoleID) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var existingRole = await _context.Roles.FindAsync(id);
                if (existingRole == null) return NotFound();

                // Check critical roles
                if (existingRole.RoleName == "Super Admin") 
                {
                    // Ensure Super Admin always has all permissions or don't allow changing permissions casually?
                    // User req: "Không cho chỉnh sửa hoặc xóa quyền của Super Admin"
                    // So we skip updating permissions if it's Super Admin?
                    // Or we just force all permissions?
                    // Let's block editing permissions for Super Admin in UI, but here we safeguard.
                    // For now, let's assume we update description/name partially but stick to existing permissions,
                    // or just return error if trying to modify Super Admin.
                    // Let's implement logic: "Role quyết định các chức năng người dùng được phép thao tác"
                }

                existingRole.RoleName = role.RoleName;
                existingRole.Description = role.Description;
                existingRole.Status = role.Status;
                existingRole.UpdatedAt = DateTime.Now;

                // Update permissions (unless Super Admin restrictions logic applied)
                 if (existingRole.RoleName != "Super Admin" && existingRole.RoleName != "Admin") 
                {
                    // Allow full edit
                     existingRole.PermissionList = selectedPermissions ?? new List<string>();
                }
                else if (existingRole.RoleName == "Admin" || existingRole.RoleName == "Super Admin")
                {
                     // Maybe allow editing Admin but warn?
                     // Req: "Không cho chỉnh sửa hoặc xóa quyền của Super Admin"
                     if (existingRole.RoleName != "Super Admin")
                     {
                         existingRole.PermissionList = selectedPermissions ?? new List<string>();
                     }
                }

                _context.Update(existingRole);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoleExists(role.RoleID)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        ViewBag.AllPermissions = SystemPermissions.GetAllPermissions();
        return View(role);
    }

    // POST: Admin/Roles/Delete/5
    [HttpPost]
    [Route("Delete/{id}")]
    [Permission(SystemPermissions.Module_Role, SystemPermissions.Action_Delete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var role = await _context.Roles.Include(r => r.UserRoles).FirstOrDefaultAsync(r => r.RoleID == id);
        if (role != null)
        {
            // Protect system roles
            if (role.RoleName == "Super Admin" || role.RoleName == "Admin" || role.RoleName == "Customer" || role.RoleName == "Staff")
            {
                 // Return error or alert
                 // Since this is generic delete, maybe just ignored or return Json error if AJAX
                 // For now, basic protection check
                 TempData["Error"] = "Không thể xóa vai trò mặc định của hệ thống.";
                 return RedirectToAction(nameof(Index));
            }

            if (role.UserRoles.Any())
            {
                 TempData["Error"] = "Không thể xóa vai trò đang có người dùng sử dụng.";
                 return RedirectToAction(nameof(Index));
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool RoleExists(int id)
    {
        return _context.Roles.Any(e => e.RoleID == id);
    }
}
