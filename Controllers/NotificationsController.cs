using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Notifications
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);

        var notifications = await _context.Notifications
            .Include(n => n.Reservation)
                .ThenInclude(r => r!.Property)
            .Where(n => n.UserId == user!.Id)
            .OrderByDescending(n => n.CreatedDate)
            .ToListAsync();

        var model = new NotificationsViewModel
        {
            Notifications = notifications,
            UnreadCount = notifications.Count(n => !n.IsRead)
        };

        return View(model);
    }

    // POST: Notifications/MarkAsRead
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);

        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Notifications/MarkAllAsRead
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
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

        return RedirectToAction(nameof(Index));
    }

    // POST: Notifications/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);

        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}

public class NotificationsViewModel
{
    public List<Notification> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }
}
