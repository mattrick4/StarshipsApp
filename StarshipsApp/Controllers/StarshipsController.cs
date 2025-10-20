using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarshipsApp.Data;
using StarshipsApp.Models;

namespace StarshipsApp.Controllers
{
    public class StarshipsController : Controller
    {
        private readonly AppDbContext _context; // EF Core DbContext

        public StarshipsController(AppDbContext context)
        {
            _context = context;
        }

        // ---------- READ ----------
        // GET: Starships
        public async Task<IActionResult> Index()
        {
            // Get all starships without tracking for read-only operation
            var starships = await _context.Starships.AsNoTracking().ToListAsync();
            return View(starships);
        }

        // GET: Starships/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            // Get starship by id using helper method
            var starship = await FindStarshipAsync(id);
            if (starship == null)
            {
                return NotFound();
            }

            return View(starship);
        }

        // ---------- CREATE ----------
        // GET: Starships/Create
        public IActionResult Create() => View();

        // POST: Starships/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Starship starship)
        {
            // Return the view with the model to show validation errors
            if (!ModelState.IsValid)
            {
                return View(starship);
            }

            // Add and save the new starship
            _context.Add(starship);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ---------- UPDATE ----------
        // GET: Starships/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            // Get starship by id using helper method
            var starship = await FindStarshipAsync(id);
            if (starship == null)
            {
                return NotFound();
            }

            // Return the view with the correct starship for editing
            return View(starship);
        }

        // POST: Starships/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Starship starship)
        {
            // Return 400 when the route id does not match the model id
            if (id != starship.Id)
            {
                return BadRequest();
            }

            // Return the view with the model to show validation errors
            if (!ModelState.IsValid)
            {
                return View(starship);
            }

            // Get the starship being tracked by EF Core
            var existing = await _context.Starships.FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null)
            {
                return NotFound();
            }

            // Copy incoming values into the tracked entity to avoid duplicate tracking
            _context.Entry(existing).CurrentValues.SetValues(starship);

            try
            {
                // Save changes
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Check if the starship still exists
                if (!await StarshipExistsAsync(starship.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Redirect to Index (main dashboard screen)
            return RedirectToAction(nameof(Index));
        }

        // ---------- DELETE ----------
        // GET: Starships/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            // Get starship by id using helper method
            var starship = await FindStarshipAsync(id);
            if (starship == null)
            {
                return NotFound();
            }

            // Return the view with the correct starship for confirmation
            return View(starship);
        }

        // POST: Starships/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Find and remove the starship if it exists
            var starship = await _context.Starships.FindAsync(id);
            if (starship != null)
            {
                _context.Starships.Remove(starship);
                await _context.SaveChangesAsync();
            }

            // Redirect to Index (main dashboard screen)
            return RedirectToAction(nameof(Index));
        }

        // ---------- HELPERS ----------
        private async Task<Starship?> FindStarshipAsync(int? id)
        {
            if (id == null)
            {
                return null;
            }

            // Use AsNoTracking for read-only operations
            return await _context.Starships.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        }

        private async Task<bool> StarshipExistsAsync(int id)
        {
            // Check for existence without retrieving the entity
            return await _context.Starships.AnyAsync(e => e.Id == id);
        }
    }
}