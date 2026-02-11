using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Pages.Properties;

[Authorize(Roles = "Admin,Agent")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Property Property { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var property = await _context.Properties.FirstOrDefaultAsync(m => m.Id == id);
        
        if (property == null)
        {
            return NotFound();
        }
        
        Property = property;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Property.ModifiedDate = DateTime.Now;
        _context.Attach(Property).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            TempData["AdminSuccessMessage"] = "Le bien a été modifié avec succès.";
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PropertyExists(Property.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return RedirectToPage("./Index");
    }

    private bool PropertyExists(int id)
    {
        return _context.Properties.Any(e => e.Id == id);
    }
}