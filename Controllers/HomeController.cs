using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Controllers;

public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
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
                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    // Utilisateurs normaux vont directement voir les biens
                    return RedirectToAction("Index", "Properties");
                }
            }
        }

        // Si non authentifié, rester sur la page d'accueil
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
