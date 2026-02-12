using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Controllers;

[Authorize]
public class PropertiesController : Controller
{
    private readonly ApplicationDbContext _context;

    public PropertiesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Properties
    public async Task<IActionResult> Index(string? searchTerm, string? statusFilter, string? typeFilter)
    {
        var query = _context.Properties.AsQueryable();

        // Filtre de recherche (case insensitive)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(searchLower)
                                  || p.Address.ToLower().Contains(searchLower)
                                  || (p.City != null && p.City.ToLower().Contains(searchLower))
                                  || (p.Description != null && p.Description.ToLower().Contains(searchLower)));
        }

        // Filtre de statut
        if (!string.IsNullOrEmpty(statusFilter))
        {
            var isActive = statusFilter == "Active";
            query = query.Where(p => p.IsActive == isActive);
        }

        // Filtre de type
        if (!string.IsNullOrEmpty(typeFilter))
        {
            query = query.Where(p => p.Type == typeFilter);
        }

        var properties = await query
            .OrderByDescending(p => p.CreatedDate)
            .ToListAsync();

        ViewBag.SearchTerm = searchTerm;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.TypeFilter = typeFilter;

        return View(properties);
    }

    // GET: Properties/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var property = await _context.Properties
            .FirstOrDefaultAsync(m => m.Id == id);

        if (property == null)
        {
            return NotFound();
        }

        return View(property);
    }

    // GET: Properties/Create
    [Authorize(Roles = "Admin,Agent")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Properties/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> Create([Bind("Id,Title,Description,Type,Status,Price,Area,Bedrooms,Bathrooms,Address,City,PostalCode,ImageUrl,IsActive")] Property property)
    {
        if (!ModelState.IsValid)
        {
            return View(property);
        }

        property.CreatedDate = DateTime.Now;
        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        TempData["AdminSuccessMessage"] = "Le bien a été ajouté avec succès.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Properties/Edit/5
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var property = await _context.Properties.FindAsync(id);
        if (property == null)
        {
            return NotFound();
        }

        return View(property);
    }

    // POST: Properties/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Type,Status,Price,Area,Bedrooms,Bathrooms,Address,City,PostalCode,ImageUrl,IsActive,CreatedDate")] Property property)
    {
        if (id != property.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(property);
        }

        property.ModifiedDate = DateTime.Now;
        _context.Update(property);

        try
        {
            await _context.SaveChangesAsync();
            TempData["AdminSuccessMessage"] = "Le bien a été modifié avec succès.";
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PropertyExists(property.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private bool PropertyExists(int id)
    {
        return _context.Properties.Any(e => e.Id == id);
    }
}
