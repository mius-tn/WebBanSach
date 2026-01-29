using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WedBanSach.Constants;

namespace WedBanSach.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class PermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _module;
    private readonly string _action;

    public PermissionAttribute(string module, string action)
    {
        _module = module;
        _action = action;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetString("UserId");

        // 1. Check if user is logged in
        if (string.IsNullOrEmpty(userId))
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl = context.HttpContext.Request.Path });
            return;
        }

        // 2. Check if user is Super Admin or Admin (bypass all checks to avoid lockout on new RBAC init)
        var roleName = session.GetString("RoleName");
        if (roleName == "Super Admin" || roleName == "Admin")
        {
            return; // Allow full access
        }
        
        // Let's get permissions from Session
        
        // Let's get permissions from Session
        var permissionsJson = session.GetString("Permissions");
        List<string> permissions = new List<string>();

        if (!string.IsNullOrEmpty(permissionsJson))
        {
            try 
            {
                permissions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(permissionsJson) ?? new List<string>();
            }
            catch 
            {
                // Fallback or log error
            }
        }

        // Generate required permission string
        var requiredPermission = SystemPermissions.Generate(_module, _action);

        // 3. Check if user has permission
        // If User is SuperAdmin (maybe explicitly check role name if not in permissions list), 
        // but let's assume Super Admin has all permissions or we just check specific list.
        // Actually, let's verify if "SystemPermissions.All" is a thing? No.
        // Let's check for exact match.
        // Also, maybe support "*" wildcard? e.g. "User.*"
        
        bool hasPermission = permissions.Contains(requiredPermission) || 
                             permissions.Contains($"{_module}.*");

        if (!hasPermission)
        {
            // 4. Forbidden
            context.Result = new StatusCodeResult(403);
            // Or a ViewResult:
            // context.Result = new ViewResult { ViewName = "AccessDenied" };
        }
    }
}
