using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Controllers;

[Authorize]
public class ReservationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    // Créneaux horaires disponibles (de 9h à 18h par intervalles d'1h)
    private static readonly List<string> AllTimeSlots = new()
    {
        "09:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00", "18:00"
    };

    public ReservationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Reservations (Admin/Agent View)
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, DateTime? dateFrom, DateTime? dateTo)
    {
        // Fetch data from database
        var query = _context.Reservations
            .Include(r => r.Property)
            .Include(r => r.User)
            .AsQueryable();

        // Filtre de recherche (case insensitive)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(r => (r.Property != null && r.Property.Title.ToLower().Contains(searchLower))
                                  || (r.User != null && r.User.FullName != null && r.User.FullName.ToLower().Contains(searchLower))
                                  || (r.User != null && r.User.Email != null && r.User.Email.ToLower().Contains(searchLower)));
        }

        // Filtre de statut
        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(r => r.Status == statusFilter);
        }

        // Filtre de date
        if (dateFrom.HasValue)
        {
            query = query.Where(r => r.ReservationDate.Date >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(r => r.ReservationDate.Date <= dateTo.Value.Date);
        }

        var reservations = (await query.ToListAsync())
            .OrderBy(r =>
            {
                // Aujourd'hui first (0)
                if (r.ReservationDate.Date == DateTime.Today) return 0;
                // Then upcoming (1)
                if (r.ReservationDate.Date > DateTime.Today) return 1;
                // Then past (2)
                return 2;
            })
            .ThenBy(r => r.ReservationDate)
            .ThenBy(r => r.TimeSlot)
            .ToList();

        var model = new ReservationsIndexViewModel
        {
            Reservations = reservations,
            PendingCount = reservations.Count(r => r.Status == "En attente"),
            AcceptedCount = reservations.Count(r => r.Status == "Accepté"),
            RefusedCount = reservations.Count(r => r.Status == "Refusé"),
            TodayCount = reservations.Count(r => r.ReservationDate.Date == DateTime.Today),
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        return View(model);
    }

    // GET: Reservations/MyReservations
    public async Task<IActionResult> MyReservations()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return RedirectToAction("Index", "Home");
        }

        // Récupérer toutes les réservations de l'utilisateur connecté
        var myReservations = (await _context.Reservations
            .Include(r => r.Property)
            .Where(r => r.UserId == userId)
            .ToListAsync())
            .OrderBy(r =>
            {
                // Aujourd'hui first (0)
                if (r.ReservationDate.Date == DateTime.Today) return 0;
                // Then upcoming (1)
                if (r.ReservationDate.Date > DateTime.Today) return 1;
                // Then past (2)
                return 2;
            })
            .ThenBy(r => r.ReservationDate)
            .ThenBy(r => r.TimeSlot)
            .ToList();

        var model = new MyReservationsViewModel
        {
            MyReservations = myReservations,
            PendingCount = myReservations.Count(r => r.Status == "En attente"),
            AcceptedCount = myReservations.Count(r => r.Status == "Accepté"),
            UpcomingCount = myReservations.Count(r => r.ReservationDate >= DateTime.Today && r.Status == "Accepté")
        };

        return View(model);
    }

    // GET: Reservations/Create?propertyId=1
    [Authorize(Roles = "User")]
    public async Task<IActionResult> Create(int propertyId)
    {
        var property = await _context.Properties.FindAsync(propertyId);

        if (property == null)
        {
            return NotFound();
        }

        var model = new CreateReservationViewModel
        {
            Property = property,
            PropertyId = propertyId,
            ReservationDate = DateTime.Today.AddDays(1),
            AvailableTimeSlots = await FetchAvailableTimeSlots(propertyId, DateTime.Today.AddDays(1))
        };

        return View(model);
    }

    // POST: Reservations/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> Create(CreateReservationViewModel model)
    {
        Console.WriteLine("========== CREATE RESERVATION POST CALLED ==========");
        Console.WriteLine($"PropertyId: {model.PropertyId}");
        Console.WriteLine($"ReservationDate: {model.ReservationDate}");
        Console.WriteLine($"TimeSlot: '{model.TimeSlot}'");
        Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

        if (!ModelState.IsValid)
        {
            Console.WriteLine("ModelState ERRORS:");
            foreach (var error in ModelState)
            {
                if (error.Value.Errors.Any())
                {
                    Console.WriteLine($"  {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }
        }

        var property = await _context.Properties.FindAsync(model.PropertyId);

        if (property == null)
        {
            return NotFound();
        }

        if (model.ReservationDate < DateTime.Today)
        {
            ModelState.AddModelError("ReservationDate", "La date de réservation doit être dans le futur.");
        }

        if (string.IsNullOrEmpty(model.TimeSlot))
        {
            ModelState.AddModelError("TimeSlot", "Veuillez sélectionner un créneau horaire.");
        }

        if (!ModelState.IsValid)
        {
            model.Property = property;
            model.AvailableTimeSlots = await FetchAvailableTimeSlots(model.PropertyId, model.ReservationDate);
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);

        // Vérifier si le créneau est toujours disponible
        var timeSpan = TimeSpan.Parse(model.TimeSlot);
        var existingReservation = await _context.Reservations
            .AnyAsync(r => r.PropertyId == model.PropertyId &&
                          r.ReservationDate.Date == model.ReservationDate.Date &&
                          r.TimeSlot == timeSpan);

        if (existingReservation)
        {
            ModelState.AddModelError("", "Ce créneau n'est plus disponible. Veuillez en choisir un autre.");
            model.Property = property;
            model.AvailableTimeSlots = await FetchAvailableTimeSlots(model.PropertyId, model.ReservationDate);
            return View(model);
        }

        var reservation = new Reservation
        {
            PropertyId = model.PropertyId,
            UserId = user!.Id,
            ReservationDate = model.ReservationDate,
            TimeSlot = timeSpan,
            Status = "En attente",
            CreatedDate = DateTime.Now
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        // Créer des notifications pour tous les admins et agents
        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        var agentUsers = await _userManager.GetUsersInRoleAsync("Agent");
        var allAdmins = adminUsers.Union(agentUsers).ToList();

        foreach (var admin in allAdmins)
        {
            var notification = new Notification
            {
                UserId = admin.Id,
                ReservationId = reservation.Id,
                Title = "Nouvelle réservation",
                Message = $"Nouvelle demande de visite pour '{property.Title}' le {reservation.ReservationDate:dd/MM/yyyy} à {reservation.TimeSlot:hh\\:mm}.",
                CreatedDate = DateTime.Now
            };
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();

        TempData["UserSuccessMessage"] = "Votre réservation a été créée avec succès ! Vous recevrez une notification une fois qu'elle sera traitée.";
        return RedirectToAction(nameof(MyReservations));
    }

    // GET: Reservations/GetAvailableTimeSlots?propertyId=1&date=2024-01-01
    [HttpGet]
    public async Task<IActionResult> GetAvailableTimeSlots(int propertyId, DateTime date)
    {
        var availableSlots = await FetchAvailableTimeSlots(propertyId, date);
        return Json(availableSlots);
    }

    // POST: Reservations/Cancel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int reservationId)
    {
        var userId = _userManager.GetUserId(User);

        var reservation = await _context.Reservations
            .Include(r => r.Property)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

        if (reservation == null)
        {
            TempData["ErrorMessage"] = "Réservation introuvable.";
            return RedirectToAction(nameof(MyReservations));
        }

        // Vérifier que la réservation n'est pas déjà annulée ou refusée
        if (reservation.Status == "Annulée" || reservation.Status == "Refusé")
        {
            TempData["ErrorMessage"] = "Cette réservation ne peut pas être annulée.";
            return RedirectToAction(nameof(MyReservations));
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

        TempData["UserSuccessMessage"] = "Votre réservation a été annulée avec succès.";
        return RedirectToAction(nameof(MyReservations));
    }

    // POST: Reservations/Accept
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> Accept(int reservationId, string? adminRemark)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Property)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null)
        {
            TempData["AdminErrorMessage"] = "Réservation introuvable.";
            return RedirectToAction(nameof(Index));
        }

        // Vérifier conflit
        var conflictingReservations = await _context.Reservations
            .Where(r => r.PropertyId == reservation.PropertyId
                     && r.ReservationDate.Date == reservation.ReservationDate.Date
                     && r.TimeSlot == reservation.TimeSlot
                     && r.Status == "Accepté"
                     && r.Id != reservationId)
            .ToListAsync();

        if (conflictingReservations.Any())
        {
            TempData["AdminErrorMessage"] = "Impossible d'accepter : une autre réservation est déjà acceptée pour ce créneau.";
            return RedirectToAction(nameof(Index));
        }

        reservation.Status = "Accepté";
        reservation.AdminRemark = adminRemark;
        reservation.ModifiedDate = DateTime.Now;
        await _context.SaveChangesAsync();

        // Notification à l'utilisateur
        var notification = new Notification
        {
            UserId = reservation.UserId,
            ReservationId = reservation.Id,
            Title = "Réservation acceptée",
            Message = $"Votre réservation pour '{reservation.Property?.Title}' le {reservation.ReservationDate:dd/MM/yyyy} à {reservation.TimeSlot:hh\\:mm} a été acceptée."
                     + (!string.IsNullOrEmpty(adminRemark) ? $" Remarque: {adminRemark}" : ""),
            CreatedDate = DateTime.Now
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        TempData["AdminSuccessMessage"] = "La réservation a été acceptée et l'utilisateur a été notifié.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Reservations/Refuse
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> Refuse(int reservationId, string? adminRemark)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Property)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null)
        {
            TempData["AdminErrorMessage"] = "Réservation introuvable.";
            return RedirectToAction(nameof(Index));
        }

        reservation.Status = "Refusé";
        reservation.AdminRemark = adminRemark;
        reservation.ModifiedDate = DateTime.Now;
        await _context.SaveChangesAsync();

        // Notification à l'utilisateur
        var notification = new Notification
        {
            UserId = reservation.UserId,
            ReservationId = reservation.Id,
            Title = "Réservation refusée",
            Message = $"Votre réservation pour '{reservation.Property?.Title}' le {reservation.ReservationDate:dd/MM/yyyy} à {reservation.TimeSlot:hh\\:mm} a été refusée."
                     + (!string.IsNullOrEmpty(adminRemark) ? $" Raison: {adminRemark}" : ""),
            CreatedDate = DateTime.Now
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        TempData["AdminSuccessMessage"] = "La réservation a été refusée et l'utilisateur a été notifié.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<string>> FetchAvailableTimeSlots(int propertyId, DateTime date)
    {
        var reservedSlots = await _context.Reservations
            .Where(r => r.PropertyId == propertyId && r.ReservationDate.Date == date.Date)
            .Select(r => r.TimeSlot.ToString(@"hh\:mm"))
            .ToListAsync();

        return AllTimeSlots.Where(ts => !reservedSlots.Contains(ts)).ToList();
    }
}

// View Models
public class ReservationsIndexViewModel
{
    public List<Reservation> Reservations { get; set; } = new();
    public int PendingCount { get; set; }
    public int AcceptedCount { get; set; }
    public int RefusedCount { get; set; }
    public int TodayCount { get; set; }
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class MyReservationsViewModel
{
    public List<Reservation> MyReservations { get; set; } = new();
    public int PendingCount { get; set; }
    public int AcceptedCount { get; set; }
    public int UpcomingCount { get; set; }
}

public class CreateReservationViewModel
{
    public Property? Property { get; set; }
    public int PropertyId { get; set; }
    public DateTime ReservationDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public List<string> AvailableTimeSlots { get; set; } = new();
}
