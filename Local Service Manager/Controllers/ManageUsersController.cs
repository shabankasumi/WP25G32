using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Local_Service_Manager.Data;
using Local_Service_Manager.Models;
using Local_Service_Manager.Models.ViewModels;

namespace Local_Service_Manager.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManageUsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        private const int PageSize = 10;

        public ManageUsersController(UserManager<IdentityUser> userManager,
                                     RoleManager<IdentityRole> roleManager,
                                     ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // INDEX search,sort,page like Services
        public async Task<IActionResult> Index(string search, string emailConfirmed, bool onlyWorkers, string sortOrder, int page = 1)
        {
            var q = _userManager.Users.AsQueryable();

            if (emailConfirmed == "confirmed") q = q.Where(u => u.EmailConfirmed);
            else if (emailConfirmed == "not") q = q.Where(u => !u.EmailConfirmed);
            ViewData["CurrentEmailConfirmed"] = emailConfirmed ?? "";

            if (onlyWorkers)
            {
                var workerIds = _context.UserPermissions.Where(p => p.CanPostServices).Select(p => p.UserId);
                q = q.Where(u => workerIds.Contains(u.Id));
            }
            ViewData["OnlyWorkers"] = onlyWorkers;

            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(u =>
                    (u.Email != null && u.Email.Contains(search)) ||
                    (u.UserName != null && u.UserName.Contains(search)));

                ViewData["CurrentSearch"] = search;
            }
            else ViewData["CurrentSearch"] = "";

            ViewData["EmailSort"] = string.IsNullOrEmpty(sortOrder) ? "email_desc" : "";
            ViewData["UserNameSort"] = sortOrder == "uname" ? "uname_desc" : "uname";
            ViewData["CurrentSort"] = sortOrder;

            q = sortOrder switch
            {
                "email_desc" => q.OrderByDescending(u => u.Email),
                "uname" => q.OrderBy(u => u.UserName),
                "uname_desc" => q.OrderByDescending(u => u.UserName),
                _ => q.OrderBy(u => u.Email),
            };

            var totalItems = await q.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / PageSize);

            var users = await q.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
            var userIds = users.Select(u => u.Id).ToList();

            // Pull permissions in one query
            var perms = await _context.UserPermissions
                .Where(p => userIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId, p => p);

            // Ensure every listed user has a row in UserPermissions (so admin can toggle easily)
            var missing = userIds.Where(id => !perms.ContainsKey(id)).ToList();
            if (missing.Any())
            {
                foreach (var id in missing)
                {
                    _context.UserPermissions.Add(new UserPermission { UserId = id, CanPostServices = false });
                }
                await _context.SaveChangesAsync();

                perms = await _context.UserPermissions
                    .Where(p => userIds.Contains(p.UserId))
                    .ToDictionaryAsync(p => p.UserId, p => p);
            }

            var vm = users.Select(u => new ManageUserRowVm
            {
                Id = u.Id,
                Email = u.Email,
                UserName = u.UserName,
                EmailConfirmed = u.EmailConfirmed,
                CanPostServices = perms.TryGetValue(u.Id, out var up) && up.CanPostServices
            }).ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(vm);
        }

        //  CREATE 
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(x => x).ToListAsync();
            return View(new IdentityUser());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IdentityUser user, string password, string role)
        {
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(x => x).ToListAsync();

            if (string.IsNullOrWhiteSpace(user.Email))
                ModelState.AddModelError("Email", "Email is required.");

            if (string.IsNullOrWhiteSpace(user.UserName))
                ModelState.AddModelError("UserName", "UserName is required.");

            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("password", "Password is required.");

            if (!ModelState.IsValid)
                return View(user);

            //  duplicate email
            var existing = await _userManager.FindByEmailAsync(user.Email);
            if (existing != null)
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(user);
            }

            user.EmailConfirmed = true;

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(user);
            }

            // assign role
            if (!string.IsNullOrWhiteSpace(role) && await _roleManager.RoleExistsAsync(role))
                await _userManager.AddToRoleAsync(user, role);

            return RedirectToAction(nameof(Index));
        }

        // ===== EDIT =====
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(x => x).ToListAsync();
            ViewBag.CurrentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "";

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string email, string userName, string phoneNumber, string role, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name!).OrderBy(x => x).ToListAsync();

            // update fields
            user.Email = email;
            user.UserName = userName;
            user.PhoneNumber = phoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var e in updateResult.Errors) ModelState.AddModelError("", e.Description);
                ViewBag.CurrentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "";
                return View(user);
            }

            // role update 
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!string.IsNullOrWhiteSpace(role) && await _roleManager.RoleExistsAsync(role))
                await _userManager.AddToRoleAsync(user, role);

            // optional password reset
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (!passResult.Succeeded)
                {
                    foreach (var e in passResult.Errors) ModelState.AddModelError("", e.Description);
                    ViewBag.CurrentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "";
                    return View(user);
                }
            }

            return RedirectToAction(nameof(Index));
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCanPostServices(string id, bool value)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var perm = await _context.UserPermissions.FirstOrDefaultAsync(p => p.UserId == id);
            if (perm == null)
            {
                perm = new UserPermission { UserId = id, CanPostServices = value };
                _context.UserPermissions.Add(perm);
            }
            else
            {
                perm.CanPostServices = value;
                _context.UserPermissions.Update(perm);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

//DELETE 
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
                await _userManager.DeleteAsync(user);

            return RedirectToAction(nameof(Index));
        }
    }
}
