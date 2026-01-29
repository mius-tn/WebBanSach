namespace WedBanSach.Constants;

public static class SystemPermissions
{
    public const string Module_User = "User";
    public const string Module_Role = "Role";
    public const string Module_Product = "Product"; // Covers Books
    public const string Module_Order = "Order";
    public const string Module_Category = "Category";
    public const string Module_Report = "Report";
    public const string Module_Setting = "Setting"; // Covers basic settings
    
    // New Modules
    public const string Module_Author = "Author";
    public const string Module_Publisher = "Publisher";
    public const string Module_Shipping = "Shipping";
    public const string Module_Payment = "Payment";
    public const string Module_Review = "Review";
    public const string Module_Promotion = "Promotion";
    public const string Module_Inventory = "Inventory";
    public const string Module_Chat = "Chat";

    public const string Action_View = "View";
    public const string Action_Create = "Create";
    public const string Action_Update = "Update";
    public const string Action_Delete = "Delete";

    // Helper to generate permission string, e.g. "User.View"
    public static string Generate(string module, string action)
    {
        return $"{module}.{action}";
    }

    // Get all available permissions grouped by module
    public static Dictionary<string, List<string>> GetAllPermissions()
    {
        var modules = new List<string> 
        { 
            Module_User, Module_Role, Module_Product, Module_Order, 
            Module_Category, Module_Report, Module_Setting,
            Module_Author, Module_Publisher, Module_Shipping, Module_Payment,
            Module_Review, Module_Promotion, Module_Inventory, Module_Chat
        };
        
        var actions = new List<string> 
        { 
            Action_View, Action_Create, Action_Update, Action_Delete 
        };

        var permissions = new Dictionary<string, List<string>>();

        foreach (var module in modules)
        {
            permissions[module] = new List<string>();
            foreach (var action in actions)
            {
                permissions[module].Add(Generate(module, action));
            }
        }

        return permissions;
    }
}
