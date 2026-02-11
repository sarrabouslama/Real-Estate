namespace RealEstateAdmin.Data;

public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    
    public int ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    public bool IsRead { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
