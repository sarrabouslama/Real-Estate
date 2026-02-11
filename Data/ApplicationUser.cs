using Microsoft.AspNetCore.Identity;

namespace RealEstateAdmin.Data;

public class ApplicationUser : IdentityUser
{
    public string? Phone { get; set; }
    public string? FullName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? RegistrationDate { get; set; } = DateTime.Now;
    public DateTime? LastLoginDate { get; set; }
    
    // Navigation properties
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}