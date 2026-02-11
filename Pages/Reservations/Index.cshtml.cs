using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Pages.Reservations;

[Authorize(Roles = "Admin,Agent")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Reservation> Reservations { get; set; } = new();
    public int PendingCount { get; set; }
    public int AcceptedCount { get; set; }
    public int RefusedCount { get; set; }
    public int TodayCount { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public DateTime? DateFrom { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public DateTime? DateTo { get; set; }

    [BindProperty]
    public int ReservationId { get; set; }
    
    [BindProperty]
    public string? AdminRemark { get; set; }
    
    [BindProperty]
    public bool ConfirmAccept { get; set; }

    public async Task OnGetAsync()
    {
        // Fetch data from database
        var query = _context.Reservations
            .Include(r => r.Property)
            .Include(r => r.User)
            .AsQueryable();
        
        // Filtre de recherche (case insensitive)
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchLower = SearchTerm.ToLower();
            query = query.Where(r => r.Property.Title.ToLower().Contains(searchLower)
                                  || r.User.FullName.ToLower().Contains(searchLower)
                                  || r.User.Email.ToLower().Contains(searchLower));
        }
        
        // Filtre de statut
        if (!string.IsNullOrEmpty(StatusFilter))
        {
            query = query.Where(r => r.Status == StatusFilter);
        }
        
        // Filtre de date
        if (DateFrom.HasValue)
        {
            query = query.Where(r => r.ReservationDate.Date >= DateFrom.Value.Date);
        }
        
        if (DateTo.HasValue)
        {
            query = query.Where(r => r.ReservationDate.Date <= DateTo.Value.Date);
        }
        
        Reservations = (await query.ToListAsync())
            .OrderByDescending(r => r.ReservationDate)
            .ThenBy(r => r.TimeSlot)
            .ToList();

        PendingCount = Reservations.Count(r => r.Status == "En attente");
        AcceptedCount = Reservations.Count(r => r.Status == "Accepté");
        RefusedCount = Reservations.Count(r => r.Status == "Refusé");
        TodayCount = Reservations.Count(r => r.ReservationDate.Date == DateTime.Today);
    }

    // Vérifier les conflits avant d'accepter
    public async Task<IActionResult> OnPostCheckConflictAsync()
    {
        var reservation = await _context.Reservations
            .Include(r => r.User)
            .Include(r => r.Property)
            .FirstOrDefaultAsync(r => r.Id == ReservationId);

        if (reservation == null)
        {
            return new JsonResult(new { hasConflict = false });
        }

        // Vérifier s'il y a d'autres réservations acceptées proches pour le même bien et le même utilisateur
        var otherAcceptedReservations = await _context.Reservations
            .Where(r => r.Id != reservation.Id 
                && r.PropertyId == reservation.PropertyId 
                && r.UserId == reservation.UserId
                && r.Status == "Accepté")
            .ToListAsync();

        if (!otherAcceptedReservations.Any())
        {
            return new JsonResult(new { hasConflict = false });
        }

        // Trouver la réservation acceptée la plus proche dans le temps
        var currentDateTime = reservation.ReservationDate.Add(reservation.TimeSlot);
        var closestReservation = otherAcceptedReservations
            .Select(r => new 
            { 
                Reservation = r, 
                DateTime = r.ReservationDate.Add(r.TimeSlot),
                TimeDifference = Math.Abs((r.ReservationDate.Add(r.TimeSlot) - currentDateTime).TotalHours)
            })
            .OrderBy(x => x.TimeDifference)
            .FirstOrDefault();

        if (closestReservation != null)
        {
            var timeDiff = closestReservation.DateTime - currentDateTime;
            var days = Math.Abs(timeDiff.Days);
            var hours = Math.Abs(timeDiff.Hours);
            
            string timeMessage = "";
            if (days > 0)
            {
                timeMessage = $"{days} jour{(days > 1 ? "s" : "")}";
                if (hours > 0)
                {
                    timeMessage += $" et {hours} heure{(hours > 1 ? "s" : "")}";
                }
            }
            else if (hours > 0)
            {
                timeMessage = $"{hours} heure{(hours > 1 ? "s" : "")}";
            }
            else
            {
                var minutes = Math.Abs(timeDiff.Minutes);
                timeMessage = $"{minutes} minute{(minutes > 1 ? "s" : "")}";
            }
            
            var direction = timeDiff.TotalHours > 0 ? "après" : "avant";
            var warningMessage = $"L'utilisateur a une autre réservation acceptée pour ce bien {timeMessage} {direction} " +
                                $"({closestReservation.Reservation.ReservationDate:dd/MM/yyyy} à {closestReservation.Reservation.TimeSlot:hh\\:mm}).";
            
            return new JsonResult(new { hasConflict = true, message = warningMessage });
        }

        return new JsonResult(new { hasConflict = false });
    }

    public async Task<IActionResult> OnPostAcceptAsync()
    {
        var reservation = await _context.Reservations
            .Include(r => r.User)
            .Include(r => r.Property)
            .FirstOrDefaultAsync(r => r.Id == ReservationId);

        if (reservation != null)
        {
            reservation.Status = "Accepté";
            reservation.AdminRemark = AdminRemark;
            reservation.ModifiedDate = DateTime.Now;
            
            // Créer une notification pour l'utilisateur
            var notification = new Notification
            {
                UserId = reservation.UserId,
                ReservationId = reservation.Id,
                Title = "Réservation acceptée",
                Message = $"Votre réservation pour le {reservation.ReservationDate:dd/MM/yyyy} à {reservation.TimeSlot:hh\\:mm} a été acceptée."
                         + (!string.IsNullOrEmpty(AdminRemark) ? $" Remarque: {AdminRemark}" : ""),
                CreatedDate = DateTime.Now
            };
            
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            
            TempData["AdminSuccessMessage"] = "Réservation acceptée avec succès.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRefuseAsync()
    {
        var reservation = await _context.Reservations
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == ReservationId);

        if (reservation != null)
        {
            reservation.Status = "Refusé";
            reservation.AdminRemark = AdminRemark;
            reservation.ModifiedDate = DateTime.Now;
            
            // Créer une notification pour l'utilisateur
            var notification = new Notification
            {
                UserId = reservation.UserId,
                ReservationId = reservation.Id,
                Title = "Réservation refusée",
                Message = $"Votre réservation pour le {reservation.ReservationDate:dd/MM/yyyy} à {reservation.TimeSlot:hh\\:mm} a été refusée."
                         + (!string.IsNullOrEmpty(AdminRemark) ? $" Raison: {AdminRemark}" : ""),
                CreatedDate = DateTime.Now
            };
            
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            
            TempData["AdminSuccessMessage"] = "Réservation refusée.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var reservation = await _context.Reservations.FindAsync(id);

        if (reservation != null)
        {
            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
            TempData["AdminSuccessMessage"] = "Réservation supprimée.";
        }

        return RedirectToPage();
    }
}
