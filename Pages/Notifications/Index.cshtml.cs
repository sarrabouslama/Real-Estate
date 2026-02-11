using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Pages.Notifications;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<Notification> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        
        Notifications = await _context.Notifications
            .Include(n => n.Reservation)
                .ThenInclude(r => r!.Property)
            .Where(n => n.UserId == user!.Id)
            .OrderByDescending(n => n.CreatedDate)
            .ToListAsync();

        UnreadCount = Notifications.Count(n => !n.IsRead);
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        
        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllAsReadAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        
        var notifications = await _context.Notifications
            .Where(n => n.UserId == user!.Id && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();
        TempData["UserSuccessMessage"] = "Toutes les notifications ont été marquées comme lues.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
