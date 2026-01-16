using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using WedBanSach.Data;
using WedBanSach.Helpers;

namespace WedBanSach.Attributes;

public class AuthorizeAdminAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var dbContext = httpContext.RequestServices.GetRequiredService<BookStoreDbContext>();

        var user = await AuthHelper.GetCurrentUserAsync(httpContext, dbContext);

        if (user == null || !AuthHelper.IsStaff(user))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        await next();
    }
}
