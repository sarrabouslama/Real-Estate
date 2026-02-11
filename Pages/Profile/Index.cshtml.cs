using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RealEstateAdmin.Data;
using System.ComponentModel.DataAnnotations;

namespace RealEstateAdmin.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
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

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Impossible de charger l'utilisateur avec l'ID '{_userManager.GetUserId(User)}'.");
            }

            Input = new InputModel
            {
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Phone = user.Phone,
                Address = user.Address,
                City = user.City
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Impossible de charger l'utilisateur avec l'ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Input.Email != user.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, Input.Email);
                if (!setEmailResult.Succeeded)
                {
                    StatusMessage = "Erreur lors de la mise à jour de l'email.";
                    return RedirectToPage();
                }
            }

            user.FullName = Input.FullName;
            user.Phone = Input.Phone;
            user.Address = Input.Address;
            user.City = Input.City;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                StatusMessage = "Erreur lors de la mise à jour du profil.";
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Votre profil a été mis à jour avec succès.";
            return RedirectToPage();
        }
    }
}
