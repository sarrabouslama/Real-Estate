using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;
using System.Security.Claims;

namespace RealEstateAdmin.ViewComponents;

public class NotificationBadgeViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public NotificationBadgeViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Content("0");
        }

        var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Content("0");
        }

        var unreadCount = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();

        return Content(unreadCount.ToString());
    }
}
