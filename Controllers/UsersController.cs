using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;

    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    // GET: Users
    public async Task<IActionResult> Index(string? searchTerm, string? roleFilter, string? statusFilter)
    {
        var users = await _userManager.Users.ToListAsync();
        var userViewModels = new List<UserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var reservationCount = await _context.Reservations.CountAsync(r => r.UserId == user.Id);

            userViewModels.Add(new UserViewModel
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
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            userViewModels = userViewModels.Where(u => u.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                                  || u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                                  || u.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                         .ToList();
        }

        if (!string.IsNullOrEmpty(roleFilter))
        {
            userViewModels = userViewModels.Where(u => u.Role == roleFilter).ToList();
        }

        if (!string.IsNullOrEmpty(statusFilter))
        {
            var isActive = statusFilter == "Active";
            userViewModels = userViewModels.Where(u => u.IsActive == isActive).ToList();
        }

        ViewBag.SearchTerm = searchTerm;
        ViewBag.RoleFilter = roleFilter;
        ViewBag.StatusFilter = statusFilter;

        return View(userViewModels);
    }

    // GET: Users/Edit/5
    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        var roles = _roleManager.Roles.Select(r => r.Name).ToList();
        ViewBag.Roles = new SelectList(roles!);

        var userRoles = await _userManager.GetRolesAsync(user);
        var currentRole = userRoles.FirstOrDefault() ?? "";

        var model = new EditUserViewModel
        {
            User = user,
            SelectedRole = currentRole,
            CurrentRole = currentRole
        };

        return View(model);
    }

    // POST: Users/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (string.IsNullOrEmpty(model.User.Email) || string.IsNullOrEmpty(model.User.UserName))
        {
            ModelState.AddModelError("", "Email et nom d'utilisateur sont requis.");
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();
            ViewBag.Roles = new SelectList(roles!);
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.User.Id);

        if (user == null)
        {
            return NotFound();
        }

        // Mettre à jour les informations
        user.Email = model.User.Email;
        user.UserName = model.User.UserName;
        user.FullName = model.User.FullName;
        user.Phone = model.User.Phone;
        user.Address = model.User.Address;
        user.City = model.User.City;
        user.IsActive = model.User.IsActive;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            var roles = _roleManager.Roles.Select(r => r.Name).ToList();
            ViewBag.Roles = new SelectList(roles!);
            return View(model);
        }

        // Mettre à jour le rôle si changé
        var currentRoles = await _userManager.GetRolesAsync(user);

        if (!string.IsNullOrEmpty(model.SelectedRole) && !currentRoles.Contains(model.SelectedRole))
        {
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            await _userManager.AddToRoleAsync(user, model.SelectedRole);
        }

        TempData["SuccessMessage"] = "L'utilisateur a été modifié avec succès.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Users/ToggleActive
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Utilisateur introuvable.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        TempData["SuccessMessage"] = user.IsActive
            ? "L'utilisateur a été activé."
            : "L'utilisateur a été désactivé.";

        return RedirectToAction(nameof(Index));
    }

    // POST: Users/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Utilisateur introuvable.";
            return RedirectToAction(nameof(Index));
        }

        // Générer un mot de passe temporaire
        var tempPassword = "TempPass123!";

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Le mot de passe a été réinitialisé. Nouveau mot de passe: {tempPassword}";
        }
        else
        {
            TempData["ErrorMessage"] = "Erreur lors de la réinitialisation du mot de passe.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Users/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Utilisateur introuvable.";
            return RedirectToAction(nameof(Index));
        }

        // Vérifier si l'utilisateur a des réservations
        var hasReservations = await _context.Reservations.AnyAsync(r => r.UserId == id);

        if (hasReservations)
        {
            TempData["ErrorMessage"] = "Impossible de supprimer cet utilisateur car il a des réservations associées.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "L'utilisateur a été supprimé avec succès.";
        }
        else
        {
            TempData["ErrorMessage"] = "Erreur lors de la suppression de l'utilisateur.";
        }

        return RedirectToAction(nameof(Index));
    }
}

// View Models
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

public class EditUserViewModel
{
    public ApplicationUser User { get; set; } = default!;
    public string SelectedRole { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;
}
