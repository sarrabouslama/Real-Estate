using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Pages;

public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Si l'utilisateur est authentifié, rediriger vers le bon dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                
                // Rediriger selon le rôle
                if (roles.Contains("Admin") || roles.Contains("Agent"))
                {
                    return RedirectToPage("/Dashboard/Index");
                }
                else
                {
                    // Utilisateurs normaux vont directement voir les biens
                    return RedirectToPage("/Properties/Index");
                }
            }
        }
        
        // Si non authentifié, rester sur la page d'accueil
        return Page();
    }
}