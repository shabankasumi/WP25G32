using Local_Service_Manager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class ServicePublicController : Controller
{
    private readonly ApplicationDbContext _context;
    private const int PageSize = 10;

    public ServicePublicController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Public index only active services search filter sort pagination 
    public async Task<IActionResult> Index(string search, string category, string sortOrder, int page = 1)
    {
        // Remember last filters 
        if (search != null) HttpContext.Session.SetString("svc_search", search);
        if (category != null) HttpContext.Session.SetString("svc_category", category);

        search ??= HttpContext.Session.GetString("svc_search") ?? "";
        category ??= HttpContext.Session.GetString("svc_category") ?? "";

        var q = _context.Services.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(s => s.Name.Contains(search) || s.Description.Contains(search));

        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(s => s.Category == category);

        ViewData["CurrentSearch"] = search;
        ViewData["CurrentCategory"] = category;

        ViewData["NameSort"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
        ViewData["DateSort"] = sortOrder == "date" ? "date_desc" : "date";
        ViewData["CurrentSort"] = sortOrder;

        q = sortOrder switch
        {
            "name_desc" => q.OrderByDescending(s => s.Name),
            "date" => q.OrderBy(s => s.CreatedAt),
            "date_desc" => q.OrderByDescending(s => s.CreatedAt),
            _ => q.OrderBy(s => s.Name)
        };

        var totalItems = await q.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / PageSize);

        var services = await q.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        ViewBag.TotalPages = totalPages;
        ViewBag.CurrentPage = page;
        ViewBag.Categories = await _context.Services
    .Where(s => s.IsActive && !string.IsNullOrEmpty(s.Category))
    .Select(s => s.Category)
    .Distinct()
    .OrderBy(c => c)
    .ToListAsync();


        return View(services);
    }

    public async Task<IActionResult> Details(int id)
    {
        var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
        if (service == null) return NotFound();
        return View(service);
    }
}

