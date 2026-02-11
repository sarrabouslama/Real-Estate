using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;
using System.Text.Json;

namespace RealEstateAdmin.Pages.Dashboard;

[Authorize(Roles = "Admin,Agent")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Statistiques principales
    public int TotalProperties { get; set; }
    public int PropertiesForSale { get; set; }
    public int PropertiesForRent { get; set; }
    public int UnreadMessages { get; set; }

    // Statistiques détaillées
    public int ActiveProperties { get; set; }
    public int InactiveProperties { get; set; }
    public int TotalMessages { get; set; }
    public int TotalUsers { get; set; }

    // Données pour les graphiques
    public string PropertyTypeLabels { get; set; } = "[]";
    public string PropertyTypeData { get; set; } = "[]";
    public string PropertyStatusLabels { get; set; } = "[]";
    public string PropertyStatusData { get; set; } = "[]";

    // Activités récentes
    public List<Property> RecentProperties { get; set; } = new();
    public List<Message> RecentMessages { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Statistiques principales
        var properties = await _context.Properties.ToListAsync();
        TotalProperties = properties.Count;
        PropertiesForSale = properties.Count(p => p.Status == "À vendre");
        PropertiesForRent = properties.Count(p => p.Status == "À louer");

        var messages = await _context.Messages.ToListAsync();
        UnreadMessages = messages.Count(m => !m.IsRead);

        // Statistiques détaillées
        ActiveProperties = properties.Count(p => p.IsActive);
        InactiveProperties = properties.Count(p => !p.IsActive);
        TotalMessages = messages.Count;
        TotalUsers = await _userManager.Users.CountAsync();

        // Données pour graphique: Biens par Type
        var propertyTypes = properties
            .GroupBy(p => p.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToList();

        if (propertyTypes.Any())
        {
            PropertyTypeLabels = JsonSerializer.Serialize(propertyTypes.Select(pt => pt.Type));
            PropertyTypeData = JsonSerializer.Serialize(propertyTypes.Select(pt => pt.Count));
        }
        else
        {
            // Données fictives si aucun bien
            PropertyTypeLabels = JsonSerializer.Serialize(new[] { "Appartement", "Maison", "Villa", "Studio" });
            PropertyTypeData = JsonSerializer.Serialize(new[] { 15, 8, 5, 3 });
        }

        // Données pour graphique: Biens par Statut
        var propertyStatuses = properties
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToList();

        if (propertyStatuses.Any())
        {
            PropertyStatusLabels = JsonSerializer.Serialize(propertyStatuses.Select(ps => ps.Status));
            PropertyStatusData = JsonSerializer.Serialize(propertyStatuses.Select(ps => ps.Count));
        }
        else
        {
            // Données fictives si aucun bien
            PropertyStatusLabels = JsonSerializer.Serialize(new[] { "À vendre", "À louer", "Vendu", "Loué" });
            PropertyStatusData = JsonSerializer.Serialize(new[] { 12, 10, 5, 4 });
        }

        // Biens récents (5 derniers)
        RecentProperties = await _context.Properties
            .OrderByDescending(p => p.CreatedDate)
            .Take(5)
            .ToListAsync();

        // Messages récents (5 derniers)
        RecentMessages = await _context.Messages
            .OrderByDescending(m => m.Date)
            .Take(5)
            .ToListAsync();
    }
}