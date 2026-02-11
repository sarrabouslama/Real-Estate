namespace RealEstateAdmin.Data;

public class Reservation
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public Property? Property { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    public DateTime ReservationDate { get; set; }
    public TimeSpan TimeSlot { get; set; } // Heure du créneau (ex: 09:00, 10:00, etc.)
    
    public string Status { get; set; } = "En attente"; // En attente, Accepté, Refusé
    public string? AdminRemark { get; set; } // Remarque de l'admin
    
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? ModifiedDate { get; set; }
}
