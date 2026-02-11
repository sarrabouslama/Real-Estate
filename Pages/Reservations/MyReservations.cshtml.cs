using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Pages.Reservations;

[Authorize]
public class MyReservationsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MyReservationsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<Reservation> MyReservations { get; set; } = new();
    public int PendingCount { get; set; }
    public int AcceptedCount { get; set; }
    public int UpcomingCount { get; set; }

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        
        if (userId != null)
        {
            // Récupérer toutes les réservations de l'utilisateur connecté
            MyReservations = (await _context.Reservations
                .Include(r => r.Property)
                .Where(r => r.UserId == userId)
                .ToListAsync())
                .OrderByDescending(r => r.ReservationDate)
                .ThenBy(r => r.TimeSlot)
                .ToList();

            PendingCount = MyReservations.Count(r => r.Status == "En attente");
            AcceptedCount = MyReservations.Count(r => r.Status == "Accepté");
            UpcomingCount = MyReservations.Count(r => r.ReservationDate >= DateTime.Today && r.Status == "Accepté");
        }
    }

    public async Task<IActionResult> OnPostCancelAsync(int reservationId)
    {
        var userId = _userManager.GetUserId(User);
        
        var reservation = await _context.Reservations
            .Include(r => r.Property)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

        if (reservation == null)
        {
            TempData["ErrorMessage"] = "Réservation introuvable.";
            return RedirectToPage();
        }

        // Vérifier que la réservation n'est pas déjà annulée ou refusée
        if (reservation.Status == "Annulée" || reservation.Status == "Refusé")
        {
            TempData["ErrorMessage"] = "Cette réservation ne peut pas être annulée.";
            return RedirectToPage();
        }

        var wasAccepted = reservation.Status == "Accepté";
        
        // Changer le statut à "Annulée" au lieu de supprimer
        reservation.Status = "Annulée";
        reservation.ModifiedDate = DateTime.Now;
        await _context.SaveChangesAsync();
        
        // Si la réservation était acceptée, notifier tous les admins et agents
        if (wasAccepted)
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var agentUsers = await _userManager.GetUsersInRoleAsync("Agent");
            var allAdmins = adminUsers.Union(agentUsers).ToList();
            
            foreach (var admin in allAdmins)
            {
                var notification = new Notification
                {
                    UserId = admin.Id,
                    ReservationId = reservation.Id,
                    Title = "Réservation annulée par l'utilisateur",
                    Message = $"Une réservation acceptée pour '{reservation.Property?.Title}' le {reservation.ReservationDate:dd/MM/yyyy} à {reservation.TimeSlot:hh\\:mm} a été annulée par l'utilisateur.",
                    CreatedDate = DateTime.Now
                };
                _context.Notifications.Add(notification);
            }
            
            await _context.SaveChangesAsync();
        }
        
        TempData["UserSuccessMessage"] = "Réservation annulée avec succès.";
        return RedirectToPage();
    }
}
