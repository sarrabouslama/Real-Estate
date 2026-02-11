using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Pages.Users;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public EditModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [BindProperty]
    public ApplicationUser User { get; set; } = default!;
    
    [BindProperty]
    public string SelectedRole { get; set; } = string.Empty;
    
    public SelectList Roles { get; set; } = default!;
    public string CurrentRole { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string? id)
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
        
        User = user;
        
        var roles = _roleManager.Roles.Select(r => r.Name).ToList();
        Roles = new SelectList(roles!);
        
        var userRoles = await _userManager.GetRolesAsync(user);
        CurrentRole = userRoles.FirstOrDefault() ?? "";
        SelectedRole = CurrentRole;
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(User.Email) || string.IsNullOrEmpty(User.UserName))
        {
            ModelState.AddModelError("", "Email et nom d'utilisateur sont requis.");
            await LoadRoles();
            return Page();
        }

        var user = await _userManager.FindByIdAsync(User.Id);
        
        if (user == null)
        {
            return NotFound();
        }

        // Mettre à jour les informations
        user.Email = User.Email;
        user.UserName = User.UserName;
        user.FullName = User.FullName;
        user.Phone = User.Phone;
        user.Address = User.Address;
        user.City = User.City;
        user.IsActive = User.IsActive;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            await LoadRoles();
            return Page();
        }

        // Mettre à jour le rôle si changé
        var currentRoles = await _userManager.GetRolesAsync(user);
        
        if (!string.IsNullOrEmpty(SelectedRole) && !currentRoles.Contains(SelectedRole))
        {
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            await _userManager.AddToRoleAsync(user, SelectedRole);
        }

        TempData["SuccessMessage"] = "L'utilisateur a été modifié avec succès.";
        return RedirectToPage("./Index");
    }

    private async Task LoadRoles()
    {
        var roles = _roleManager.Roles.Select(r => r.Name).ToList();
        Roles = new SelectList(roles!);
        
        if (!string.IsNullOrEmpty(User.Id))
        {
            var user = await _userManager.FindByIdAsync(User.Id);
            if (user != null)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                CurrentRole = userRoles.FirstOrDefault() ?? "";
            }
        }
    }
}