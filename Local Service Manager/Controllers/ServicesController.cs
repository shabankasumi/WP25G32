using Local_Service_Manager.Data;
using Local_Service_Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Local_Service_Manager.Controllers
{
    [Authorize(Roles = "Admin")] 
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 10; 

        public ServicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ADMIN INDEX — with search,filter,sort,pagination
        public async Task<IActionResult> AdminIndex(string search, string category, string sortOrder, int page = 1)
        {
            // Start query
            var servicesQuery = _context.Services.AsQueryable();

            //  FILTER 
            if (!string.IsNullOrEmpty(search))
            {
                servicesQuery = servicesQuery.Where(s => s.Name.Contains(search));
                ViewData["CurrentSearch"] = search;
            }
            else
            {
                ViewData["CurrentSearch"] = "";
            }

            if (!string.IsNullOrEmpty(category))
            {
                servicesQuery = servicesQuery.Where(s => s.Category == category);
                ViewData["CurrentCategory"] = category;
            }
            else
            {
                ViewData["CurrentCategory"] = "";
            }

            // --- SORT -
            ViewData["NameSort"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSort"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["CurrentSort"] = sortOrder;

            servicesQuery = sortOrder switch
            {
                "name_desc" => servicesQuery.OrderByDescending(s => s.Name),
                "date" => servicesQuery.OrderBy(s => s.CreatedAt),
                "date_desc" => servicesQuery.OrderByDescending(s => s.CreatedAt),
                _ => servicesQuery.OrderBy(s => s.Name),
            };

            //  PAGINATION
            var totalItems = await servicesQuery.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / PageSize);

            var services = await servicesQuery
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(services); // Uses AdminIndex
        }

        // CREATE
       
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service)
        {
            if (!ModelState.IsValid) return View(service);

            service.CreatedAt = DateTime.UtcNow;
            service.CreatedByUserId = User?.Identity?.Name ?? "";

            _context.Services.Add(service);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AdminIndex));
        }

        // EDIT
        public async Task<IActionResult> Edit(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();
            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Service service)
        {
            if (id != service.Id) return BadRequest();

            if (!ModelState.IsValid) return View(service);

            // preserve CreatedAt/CreatedByUserId
            var existing = await _context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null) return NotFound();
            service.CreatedAt = existing.CreatedAt;
            service.CreatedByUserId = existing.CreatedByUserId;
            _context.Update(service);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AdminIndex));
        }

        // DELETE
        public async Task<IActionResult> Delete(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();
            return View(service);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(AdminIndex));
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();
            return View(service);
        }
    }
}
