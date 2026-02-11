// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "L'adresse email est requise.")]
            [EmailAddress(ErrorMessage = "L'adresse email n'est pas valide.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Le mot de passe est requis.")]
            [StringLength(100, ErrorMessage = "Le {0} doit contenir au moins {2} caractères.", MinimumLength = 4)]
            [DataType(DataType.Password)]
            [Display(Name = "Mot de passe")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmer le mot de passe")]
            [Compare("Password", ErrorMessage = "Le mot de passe et la confirmation ne correspondent pas.")]
            public string ConfirmPassword { get; set; }

            [Required(ErrorMessage = "Le nom complet est requis.")]
            [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères.")]
            [Display(Name = "Nom complet")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "Le nom d'utilisateur est requis.")]
            [StringLength(100, ErrorMessage = "Le nom d'utilisateur ne peut pas dépasser 100 caractères.")]
            [Display(Name = "Nom d'utilisateur")]
            public string UserName { get; set; }

            [Required(ErrorMessage = "Le numéro de téléphone est requis.")]
            [Phone(ErrorMessage = "Le numéro de téléphone n'est pas valide.")]
            [Display(Name = "Téléphone")]
            public string Phone { get; set; }

            [Required(ErrorMessage = "L'adresse est requise.")]
            [StringLength(200, ErrorMessage = "L'adresse ne peut pas dépasser 200 caractères.")]
            [Display(Name = "Adresse")]
            public string Address { get; set; }

            [Required(ErrorMessage = "La ville est requise.")]
            [StringLength(100, ErrorMessage = "La ville ne peut pas dépasser 100 caractères.")]
            [Display(Name = "Ville")]
            public string City { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            
            if (ModelState.IsValid)
            {
                // Vérifier l'unicité de l'email
                var existingUserByEmail = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError(string.Empty, "Cette adresse email est déjà utilisée.");
                    return Page();
                }
                
                // Vérifier l'unicité du nom d'utilisateur
                var existingUserByUsername = await _userManager.FindByNameAsync(Input.UserName);
                if (existingUserByUsername != null)
                {
                    ModelState.AddModelError(string.Empty, "Ce nom d'utilisateur est déjà utilisé.");
                    return Page();
                }
                
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                
                // Définir les propriétés supplémentaires
                user.FullName = Input.FullName;
                user.Phone = Input.Phone;
                user.PhoneNumber = Input.Phone; // Pour Identity
                user.Address = Input.Address;
                user.City = Input.City;
                user.IsActive = true; // Compte actif par défaut
                user.EmailConfirmed = true; // Email confirmé automatiquement
                user.RegistrationDate = DateTime.Now;

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("L'utilisateur a créé un nouveau compte avec mot de passe.");

                    // Assigner le rôle "User" par défaut
                    await _userManager.AddToRoleAsync(user, "User");

                    var userId = await _userManager.GetUserIdAsync(user);
                    
                    // Connexion automatique après inscription
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
