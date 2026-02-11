using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Pages.Properties;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Property> Properties { get; set; } = new List<Property>();
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? TypeFilter { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.Properties.AsQueryable();
        
        // Filtre de recherche (case insensitive)
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchLower = SearchTerm.ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(searchLower) 
                                  || p.Address.ToLower().Contains(searchLower) 
                                  || p.City.ToLower().Contains(searchLower)
                                  || p.Description.ToLower().Contains(searchLower));
        }
        
        // Filtre de statut
        if (!string.IsNullOrEmpty(StatusFilter))
        {
            var isActive = StatusFilter == "Active";
            query = query.Where(p => p.IsActive == isActive);
        }
        
        // Filtre de type
        if (!string.IsNullOrEmpty(TypeFilter))
        {
            query = query.Where(p => p.Type == TypeFilter);
        }
        
        Properties = await query
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();
    }
}