using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RealEstateAdmin.Data;

namespace RealEstateAdmin.Pages.Properties;

[Authorize(Roles = "Admin,Agent")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Property Property { get; set; } = new Property();

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Property.CreatedDate = DateTime.Now;
        _context.Properties.Add(Property);
        await _context.SaveChangesAsync();

        TempData["AdminSuccessMessage"] = "Le bien a été ajouté avec succès.";
        return RedirectToPage("./Index");
    }
}