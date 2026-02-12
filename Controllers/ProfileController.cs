using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RealEstateAdmin.Data;
using System.ComponentModel.DataAnnotations;

namespace RealEstateAdmin.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ProfileController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: Profile
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Impossible de charger l'utilisateur avec l'ID '{_userManager.GetUserId(User)}'.");
        }

        var model = new ProfileViewModel
        {
            FullName = user.FullName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone,
            Address = user.Address,
            City = user.City
        };

        return View(model);
    }

    // POST: Profile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Impossible de charger l'utilisateur avec l'ID '{_userManager.GetUserId(User)}'.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Email != user.Email)
        {
            var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
            if (!setEmailResult.Succeeded)
            {
                TempData["StatusMessage"] = "Erreur lors de la mise à jour de l'email.";
                return View(model);
            }
        }

        user.FullName = model.FullName;
        user.Phone = model.Phone;
        user.Address = model.Address;
        user.City = model.City;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            TempData["StatusMessage"] = "Erreur inattendue lors de la mise à jour du profil.";
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["StatusMessage"] = "Votre profil a été mis à jour avec succès.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Profile/ChangePassword
    public IActionResult ChangePassword()
    {
        return RedirectToAction("ChangePassword", "Account", new { area = "Identity" });
    }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Le nom complet est requis")]
    [Display(Name = "Nom complet")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "L'adresse email n'est pas valide")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Le numéro de téléphone n'est pas valide")]
    [Display(Name = "Téléphone")]
    public string? Phone { get; set; }

    [Display(Name = "Adresse")]
    public string? Address { get; set; }

    [Display(Name = "Ville")]
    public string? City { get; set; }
}
