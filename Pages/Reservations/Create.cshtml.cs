using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Pages.Reservations;

[Authorize(Roles = "User")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Property Property { get; set; } = null!;
    
    [BindProperty]
    public DateTime ReservationDate { get; set; } = DateTime.Today.AddDays(1);
    
    [BindProperty]
    public string TimeSlot { get; set; } = string.Empty;
    
    public List<string> AvailableTimeSlots { get; set; } = new();

    // Créneaux horaires disponibles (de 9h à 18h par intervalles d'1h)
    private static readonly List<string> AllTimeSlots = new()
    {
        "09:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00", "18:00"
    };

    public async Task<IActionResult> OnGetAsync(int propertyId)
    {
        var property = await _context.Properties.FindAsync(propertyId);
        
        if (property == null)
        {
            return NotFound();
        }

        Property = property;
        await LoadAvailableTimeSlots(propertyId, ReservationDate);
        
        return Page();
    }

    public async Task<IActionResult> OnGetAvailableTimeSlotsAsync(int propertyId, DateTime date)
    {
        var reservedSlots = await _context.Reservations
            .Where(r => r.PropertyId == propertyId && r.ReservationDate.Date == date.Date)
            .Select(r => r.TimeSlot.ToString(@"hh\:mm"))
            .ToListAsync();

        var availableSlots = AllTimeSlots.Where(ts => !reservedSlots.Contains(ts)).ToList();
        
        return new JsonResult(availableSlots);
    }

    public async Task<IActionResult> OnPostAsync(int propertyId)
    {
        var property = await _context.Properties.FindAsync(propertyId);
        
        if (property == null)
        {
            return NotFound();
        }

        Property = property;

        if (ReservationDate < DateTime.Today)
        {
            ModelState.AddModelError("ReservationDate", "La date de réservation doit être dans le futur.");
        }

        if (string.IsNullOrEmpty(TimeSlot))
        {
            ModelState.AddModelError("TimeSlot", "Veuillez sélectionner un créneau horaire.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAvailableTimeSlots(propertyId, ReservationDate);
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        
        // Vérifier si le créneau est toujours disponible
        var timeSpan = TimeSpan.Parse(TimeSlot);
        var existingReservation = await _context.Reservations
            .AnyAsync(r => r.PropertyId == propertyId && 
                          r.ReservationDate.Date == ReservationDate.Date && 
                          r.TimeSlot == timeSpan);

        if (existingReservation)
        {
            ModelState.AddModelError("TimeSlot", "Ce créneau n'est plus disponible. Veuillez en choisir un autre.");
            await LoadAvailableTimeSlots(propertyId, ReservationDate);
            return Page();
        }

        var reservation = new Reservation
        {
            PropertyId = propertyId,
            UserId = user!.Id,
            ReservationDate = ReservationDate,
            TimeSlot = timeSpan,
            Status = "En attente",
            CreatedDate = DateTime.Now
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        TempData["UserReservationSuccess"] = "Votre réservation a été enregistrée avec succès ! Vous recevrez une notification une fois qu'elle sera confirmée.";
        
        return RedirectToPage("/Properties/Index");
    }

    private async Task LoadAvailableTimeSlots(int propertyId, DateTime date)
    {
        var reservedSlots = await _context.Reservations
            .Where(r => r.PropertyId == propertyId && r.ReservationDate.Date == date.Date)
            .Select(r => r.TimeSlot.ToString(@"hh\:mm"))
            .ToListAsync();

        AvailableTimeSlots = AllTimeSlots.Where(ts => !reservedSlots.Contains(ts)).ToList();
    }
}
