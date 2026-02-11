using System.ComponentModel.DataAnnotations;

namespace RealEstateAdmin.Data;

public class Message
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Le nom est obligatoire")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Email invalide")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le message est obligatoire")]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
    
    public DateTime Date { get; set; } = DateTime.Now;
    
    public bool IsRead { get; set; } = false;
    
    [StringLength(100)]
    public string? Subject { get; set; }
    
    [StringLength(20)]
    public string? Phone { get; set; }
    
    // Nouveaux champs pour la gestion des réservations
    [StringLength(20)]
    public string Status { get; set; } = "En attente"; // En attente, Accepté, Refusé
    
    public int? PropertyId { get; set; }
    
    [StringLength(2000)]
    public string? Response { get; set; }
    
    public DateTime? ResponseDate { get; set; }
}