#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Areas.Identity.Controllers
{
    [Area("Identity")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
        }

        // GET: Login
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                // Find user by email to get username
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Tentative de connexion invalide.");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Utilisateur connecté.");

                    // Redirect admins and agents to dashboard if no specific return URL
                    if (string.IsNullOrEmpty(returnUrl) || returnUrl == "/" || returnUrl == "~/")
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        if (roles.Contains("Admin") || roles.Contains("Agent"))
                        {
                            return RedirectToAction("Index", "Dashboard", new { area = "" });
                        }
                        else
                        {
                            // Utilisateurs normaux vont voir les biens
                            return RedirectToAction("Index", "Properties", new { area = "" });
                        }
                    }

                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction("LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Compte utilisateur verrouillé.");
                    return RedirectToAction("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Tentative de connexion invalide.");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View(model);
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        // GET: Register
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new RegisterViewModel());
        }

        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                // Check email uniqueness
                var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError(string.Empty, "Cette adresse email est déjà utilisée.");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View(model);
                }

                // Check username uniqueness
                var existingUserByUsername = await _userManager.FindByNameAsync(model.UserName);
                if (existingUserByUsername != null)
                {
                    ModelState.AddModelError(string.Empty, "Ce nom d'utilisateur est déjà utilisé.");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View(model);
                }

                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, model.UserName, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, model.Email, CancellationToken.None);

                // Set additional properties
                user.FullName = model.FullName;
                user.Phone = model.Phone;
                user.PhoneNumber = model.Phone;
                user.Address = model.Address;
                user.City = model.City;
                user.IsActive = true;
                user.EmailConfirmed = true;
                user.RegistrationDate = DateTime.Now;

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("L'utilisateur a créé un nouveau compte avec mot de passe.");

                    // Assign default "User" role
                    await _userManager.AddToRoleAsync(user, "User");

                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // Redirect to Properties for new users
                    if (string.IsNullOrEmpty(returnUrl) || returnUrl == "/" || returnUrl == "~/")
                    {
                        return RedirectToAction("Index", "Properties", new { area = "" });
                    }

                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        // POST: Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");

            return RedirectToAction("Login", "Account", new { area = "Identity" });
        }

        // GET: ChangePassword
        [Authorize]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Impossible de charger l'utilisateur avec l'ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword)
            {
                return RedirectToAction("SetPassword");
            }

            return View(new ChangePasswordViewModel());
        }

        // POST: ChangePassword
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Impossible de charger l'utilisateur avec l'ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("L'utilisateur a changé son mot de passe avec succès.");

            TempData["StatusMessage"] = "Votre mot de passe a été modifié avec succès.";
            return RedirectToAction("Index", "Profile", new { area = "" });
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
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor.");
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

    // View Models
    public class LoginViewModel
    {
        [Required(ErrorMessage = "L'adresse email est requise.")]
        [EmailAddress(ErrorMessage = "L'adresse email n'est pas valide.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; }

        [Display(Name = "Se souvenir de moi ?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
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

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Le mot de passe actuel est requis.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe actuel")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Le nouveau mot de passe est requis.")]
        [StringLength(100, ErrorMessage = "Le {0} doit contenir au moins {2} caractères.", MinimumLength = 4)]
        [DataType(DataType.Password)]
        [Display(Name = "Nouveau mot de passe")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le nouveau mot de passe")]
        [Compare("NewPassword", ErrorMessage = "Le nouveau mot de passe et la confirmation ne correspondent pas.")]
        public string ConfirmPassword { get; set; }
    }
}
