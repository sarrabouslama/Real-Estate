using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Pages.Users;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public IndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public List<UserViewModel> Users { get; set; } = new();
    public string? SuccessMessage { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? RoleFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }

    public async Task OnGetAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var reservationCount = await _context.Reservations.CountAsync(r => r.UserId == user.Id);
            
            Users.Add(new UserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                FullName = user.FullName ?? "",
                Phone = user.Phone ?? "",
                Role = roles.FirstOrDefault() ?? "User",
                EmailConfirmed = user.EmailConfirmed,
                IsActive = user.IsActive,
                CreatedDate = user.RegistrationDate ?? DateTime.Now,
                LastLoginDate = user.LastLoginDate,
                ReservationCount = reservationCount
            });
        }
        
        // Appliquer les filtres (case insensitive)
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            Users = Users.Where(u => u.FullName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) 
                                  || u.Email.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                                  || u.UserName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                         .ToList();
        }
        
        if (!string.IsNullOrEmpty(RoleFilter))
        {
            Users = Users.Where(u => u.Role == RoleFilter).ToList();
        }
        
        if (!string.IsNullOrEmpty(StatusFilter))
        {
            var isActive = StatusFilter == "Active";
            Users = Users.Where(u => u.IsActive == isActive).ToList();
        }
        
        if (TempData["SuccessMessage"] != null)
        {
            SuccessMessage = TempData["SuccessMessage"]?.ToString();
        }
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        
        if (user == null)
        {
            TempData["ErrorMessage"] = "Utilisateur introuvable.";
            return RedirectToPage();
        }

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);
        
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = user.IsActive 
                ? "Utilisateur activé avec succès." 
                : "Utilisateur désactivé avec succès.";
        }
        else
        {
            TempData["ErrorMessage"] = "Erreur lors de la mise à jour: " + string.Join(", ", result.Errors.Select(e => e.Description));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        
        if (user == null)
        {
            TempData["ErrorMessage"] = "Utilisateur introuvable.";
            return RedirectToPage();
        }

        // Delete associated data first
        var reservations = _context.Reservations.Where(r => r.UserId == id);
        _context.Reservations.RemoveRange(reservations);
        
        var notifications = _context.Notifications.Where(n => n.UserId == id);
        _context.Notifications.RemoveRange(notifications);
        
        await _context.SaveChangesAsync();

        var result = await _userManager.DeleteAsync(user);
        
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Utilisateur supprimé avec succès.";
        }
        else
        {
            TempData["ErrorMessage"] = "Erreur lors de la suppression: " + string.Join(", ", result.Errors.Select(e => e.Description));
        }

        return RedirectToPage();
    }

    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public int ReservationCount { get; set; }
    }
}