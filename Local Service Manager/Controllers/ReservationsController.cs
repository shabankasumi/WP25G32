using Local_Service_Manager.Data;
using Local_Service_Manager.Models;
using Local_Service_Manager.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Local_Service_Manager.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private const int PageSize = 10;

        public ReservationsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        
        // USER My reservations
       
        public async Task<IActionResult> My(string search, string status, string sortOrder, int page = 1)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var q = _context.Reservations
                .Include(r => r.Service)
                .Where(r => r.UserId == userId);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(r => r.Service.Name.Contains(search));

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(r => r.Status == status);

            ViewData["CurrentSearch"] = search ?? "";
            ViewData["CurrentStatus"] = status ?? "";

            ViewData["DateSort"] = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewData["CurrentSort"] = sortOrder;

            q = sortOrder switch
            {
                "date_desc" => q.OrderByDescending(r => r.ReservationDate),
                _ => q.OrderBy(r => r.ReservationDate)
            };

            var totalItems = await q.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / PageSize);

            var items = await q.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(items);
        }

        
        // USER: Create from public services list/details
        
        [HttpGet]
        public async Task<IActionResult> Create(int serviceId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == serviceId && s.IsActive);
            if (service == null) return NotFound();

            var vm = new ReservationCreateVm
            {
                ServiceId = service.Id,
                ServiceName = service.Name,
                ReservationDate = DateTime.Today.AddDays(1)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReservationCreateVm vm)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == vm.ServiceId && s.IsActive);
            if (service == null) return NotFound();

            // Basic business rule
            if (vm.ReservationDate.Date < DateTime.Today)
                ModelState.AddModelError(nameof(vm.ReservationDate), "Reservation date must be today or in the future.");

            if (!ModelState.IsValid)
            {
                vm.ServiceName = service.Name;
                return View(vm);
            }

            var reservation = new Reservation
            {
                ServiceId = vm.ServiceId,
                UserId = userId,
                ReservationDate = vm.ReservationDate,
                Street = vm.Street,
                City = vm.City,
                Zip = vm.Zip,
                PhoneNumber = vm.PhoneNumber,
                Notes = vm.Notes,
                Status = "Pending"
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(My));
        }

        
        // USER: Edit 
    
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var reservation = await _context.Reservations.Include(r => r.Service).FirstOrDefaultAsync(r => r.Id == id);
            if (reservation == null) return NotFound();
            if (!User.IsInRole("Admin") && reservation.UserId != userId) return Forbid();

            ViewBag.Services = new SelectList(await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(), "Id", "Name", reservation.ServiceId);
            var vm = new ReservationEditVm
            {
                Id = reservation.Id,
                ServiceId = reservation.ServiceId,
                ServiceName = reservation.Service?.Name ?? "",
                ReservationDate = reservation.ReservationDate,
                Street = reservation.Street,
                City = reservation.City,
                Zip = reservation.Zip,
                PhoneNumber = reservation.PhoneNumber,
                Notes = reservation.Notes,
                Status = reservation.Status
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ReservationEditVm vm)
        {
            var userId = _userManager.GetUserId(User);
            var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == vm.Id);
            if (reservation == null) return NotFound();
            if (!User.IsInRole("Admin") && reservation.UserId != userId) return Forbid();

            if (!User.IsInRole("Admin") && reservation.Status != "Pending")
            {
                ModelState.AddModelError("", "Only pending reservations can be updated.");
            }

            if (vm.ReservationDate.Date < DateTime.Today)
                ModelState.AddModelError(nameof(vm.ReservationDate), "Reservation date must be today or in the future.");

            if (!await _context.Services.AnyAsync(s => s.Id == vm.ServiceId && s.IsActive))
                ModelState.AddModelError(nameof(vm.ServiceId), "Invalid service.");

            if (!ModelState.IsValid)
            {
                ViewBag.Services = new SelectList(await _context.Services.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(), "Id", "Name", vm.ServiceId);
                return View(vm);
            }

            reservation.ServiceId = vm.ServiceId;
            reservation.ReservationDate = vm.ReservationDate;
            reservation.Street = vm.Street;
            reservation.City = vm.City;
            reservation.Zip = vm.Zip;
            reservation.PhoneNumber = vm.PhoneNumber;
            reservation.Notes = vm.Notes;
            _context.Update(reservation);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(My));
        }

        // USER: Cancel
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == id);
            if (reservation == null) return NotFound();
            if (!User.IsInRole("Admin") && reservation.UserId != userId) return Forbid();

            reservation.Status = "Cancelled";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(My));
        }

        
        // ADMIN: Manage all reservations 
        
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string search, string status, string sortOrder, int page = 1)
        {
            var q = _context.Reservations
                .Include(r => r.Service)
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(r => r.User.Email.Contains(search) || r.Service.Name.Contains(search));

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(r => r.Status == status);

            ViewData["CurrentSearch"] = search ?? "";
            ViewData["CurrentStatus"] = status ?? "";

            ViewData["DateSort"] = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewData["CurrentSort"] = sortOrder;

            q = sortOrder switch
            {
                "date_desc" => q.OrderByDescending(r => r.ReservationDate),
                _ => q.OrderBy(r => r.ReservationDate)
            };

            var totalItems = await q.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / PageSize);
            var items = await q.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(items);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Service)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (reservation == null) return NotFound();
            return View(reservation);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, string status)
        {
            var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == id);
            if (reservation == null) return NotFound();

            reservation.Status = status;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
