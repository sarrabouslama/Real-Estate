using System.ComponentModel.DataAnnotations;

namespace RealEstateAdmin.Data;

public class Property
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Le titre est obligatoire")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Le type est obligatoire")]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty; // Appartement, Maison, Villa, etc.
    
    [Required(ErrorMessage = "Le prix est obligatoire")]
    [Range(0, double.MaxValue, ErrorMessage = "Le prix doit être positif")]
    public decimal Price { get; set; }
    
    [Required(ErrorMessage = "L'adresse est obligatoire")]
    [StringLength(300)]
    public string Address { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? City { get; set; }
    
    [StringLength(50)]
    public string? ZipCode { get; set; }
    
    [StringLength(2000)]
    public string? Description { get; set; }
    
    [Required(ErrorMessage = "Le statut est obligatoire")]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty; // À vendre, À louer, Vendu, Loué
    
    // Property details
    [Range(0, double.MaxValue)]
    public double? Area { get; set; } // Surface en m²
    
    [Range(0, int.MaxValue)]
    public int? Bedrooms { get; set; }
    
    [Range(0, int.MaxValue)]
    public int? Bathrooms { get; set; }
    
    [Range(0, int.MaxValue)]
    public int? Floor { get; set; }
    
    public bool HasParking { get; set; } = false;
    public bool HasGarden { get; set; } = false;
    public bool HasBalcony { get; set; } = false;
    public bool HasElevator { get; set; } = false;
    
    [StringLength(2000)]
    public string? ImageUrl { get; set; }
    
    // Availability & Status
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public int ViewCount { get; set; } = 0;
    
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? ModifiedDate { get; set; }
    
    // Navigation properties
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}