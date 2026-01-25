using Local_Service_Manager.Data;
using Local_Service_Manager.Models;
using Local_Service_Manager.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Local_Service_Manager.Controllers
{
    [Authorize]
    public class ServicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private const int PageSize = 10; 

        public ServicesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<bool> CanCurrentUserPostServicesAsync()
        {
            if (User.IsInRole("Admin")) return true;
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return false;
            return await _context.UserPermissions.AnyAsync(p => p.UserId == userId && p.CanPostServices);
        }

        private async Task<bool> HasServicePermissionAsync(int serviceId, bool requireEdit)
        {
            if (User.IsInRole("Admin")) return true;
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return false;

            return requireEdit
                ? await _context.ServicePermissions.AnyAsync(p => p.ServiceId == serviceId && p.UserId == userId && p.CanEdit)
                : await _context.ServicePermissions.AnyAsync(p => p.ServiceId == serviceId && p.UserId == userId && p.CanView);
        }

        // ADMIN INDEX — with search,filter,sort,pagination
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex(string search, string category, bool onlyActive, string sortOrder, int page = 1)
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

            // Checkbox filter
            if (onlyActive)
            {
                servicesQuery = servicesQuery.Where(s => s.IsActive);
            }
            ViewData["OnlyActive"] = onlyActive;

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

        // WORKER: My services (services created by this user)
        public async Task<IActionResult> MyServices(string search, string category, string sortOrder, int page = 1)
        {
            if (!await CanCurrentUserPostServicesAsync()) return Forbid();

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return Challenge();

            var q = _context.Services.Where(s => s.CreatedByUserId == userId).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(s => s.Name.Contains(search) || s.Description.Contains(search));

            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(s => s.Category == category);

            ViewData["CurrentSearch"] = search ?? "";
            ViewData["CurrentCategory"] = category ?? "";

            ViewData["NameSort"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSort"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["CurrentSort"] = sortOrder;

            q = sortOrder switch
            {
                "name_desc" => q.OrderByDescending(s => s.Name),
                "date" => q.OrderBy(s => s.CreatedAt),
                "date_desc" => q.OrderByDescending(s => s.CreatedAt),
                _ => q.OrderBy(s => s.Name),
            };

            var totalItems = await q.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / PageSize);

            var items = await q.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.Categories = await _context.Services.Select(s => s.Category).Distinct().OrderBy(x => x).ToListAsync();

            return View(items);
        }


        // CREATE
       
        public async Task<IActionResult> Create()
        {
            if (!await CanCurrentUserPostServicesAsync()) return Forbid();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service service)
        {
            if (!await CanCurrentUserPostServicesAsync()) return Forbid();
            if (!ModelState.IsValid) return View(service);

            service.CreatedAt = DateTime.UtcNow;
            service.CreatedByUserId = _userManager.GetUserId(User) ?? "";

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            // Admin goes back to AdminIndex, worker goes to MyServices
            return User.IsInRole("Admin")
                ? RedirectToAction(nameof(AdminIndex))
                : RedirectToAction(nameof(MyServices));
        }

        // EDIT
        public async Task<IActionResult> Edit(int id)
        {
            if (!await CanCurrentUserPostServicesAsync()) return Forbid();

            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (!User.IsInRole("Admin") && service.CreatedByUserId != userId && !await HasServicePermissionAsync(id, requireEdit: true))
                return Forbid();

            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Service service)
        {
            if (!await CanCurrentUserPostServicesAsync()) return Forbid();
            if (id != service.Id) return BadRequest();

            var existing = await _context.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (!User.IsInRole("Admin") && existing.CreatedByUserId != userId && !await HasServicePermissionAsync(id, requireEdit: true))
                return Forbid();

            if (!ModelState.IsValid) return View(service);

            // preserve CreatedAt/CreatedByUserId
            service.CreatedAt = existing.CreatedAt;
            service.CreatedByUserId = existing.CreatedByUserId;

            _context.Update(service);
            await _context.SaveChangesAsync();

            return User.IsInRole("Admin")
                ? RedirectToAction(nameof(AdminIndex))
                : RedirectToAction(nameof(MyServices));
        }

        // DELETE
        public async Task<IActionResult> Delete(int id)
        {
            if (!await CanCurrentUserPostServicesAsync()) return Forbid();

            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (!User.IsInRole("Admin") && service.CreatedByUserId != userId && !await HasServicePermissionAsync(id, requireEdit: true))
                return Forbid();

            return View(service);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!await CanCurrentUserPostServicesAsync()) return Forbid();

            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (!User.IsInRole("Admin") && service.CreatedByUserId != userId && !await HasServicePermissionAsync(id, requireEdit: true))
                return Forbid();

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return User.IsInRole("Admin")
                ? RedirectToAction(nameof(AdminIndex))
                : RedirectToAction(nameof(MyServices));
        }

        // ADMIN: assign per-service permissions (Requirement #22)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Permissions(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            var existing = await _context.ServicePermissions.Where(p => p.ServiceId == id).ToListAsync();

            var vm = new ServicePermissionsVm
            {
                ServiceId = id,
                ServiceName = service.Name,
                Users = users.Select(u =>
                {
                    var perm = existing.FirstOrDefault(p => p.UserId == u.Id);
                    return new ServicePermissionRowVm
                    {
                        UserId = u.Id,
                        Email = u.Email ?? u.UserName ?? u.Id,
                        CanView = perm?.CanView ?? false,
                        CanEdit = perm?.CanEdit ?? false
                    };
                }).ToList()
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Permissions(ServicePermissionsVm vm)
        {
            var service = await _context.Services.FindAsync(vm.ServiceId);
            if (service == null) return NotFound();

            var current = await _context.ServicePermissions.Where(p => p.ServiceId == vm.ServiceId).ToListAsync();

            // Upsert rows
            foreach (var row in vm.Users)
            {
                // If CanEdit true, treat CanView true as well
                if (row.CanEdit) row.CanView = true;

                var existing = current.FirstOrDefault(p => p.UserId == row.UserId);
                if (!row.CanView && !row.CanEdit)
                {
                    if (existing != null)
                        _context.ServicePermissions.Remove(existing);
                    continue;
                }

                if (existing == null)
                {
                    _context.ServicePermissions.Add(new ServicePermission
                    {
                        ServiceId = vm.ServiceId,
                        UserId = row.UserId,
                        CanView = row.CanView,
                        CanEdit = row.CanEdit
                    });
                }
                else
                {
                    existing.CanView = row.CanView;
                    existing.CanEdit = row.CanEdit;
                    _context.ServicePermissions.Update(existing);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AdminIndex));
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();
            if (!service.IsActive)
            {
                var userId = _userManager.GetUserId(User);
                if (!User.IsInRole("Admin") && service.CreatedByUserId != userId && !await HasServicePermissionAsync(id, requireEdit: false))
                    return Forbid();
            }
            return View(service);
        }
    }
}
