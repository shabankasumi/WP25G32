using Local_Service_Manager.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Local_Service_Manager.Controllers.Api
{
    [Route("api/services")]
    [ApiController]
    public class ServicesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ServicesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = false)
        {
            var q = _context.Services.AsQueryable();
            if (onlyActive) q = q.Where(s => s.IsActive);

            var items = await q
                .OrderBy(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Category,
                    s.Description,
                    s.IsActive,
                    s.CreatedAt,
                    s.CreatedByUserId
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var s = await _context.Services
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Category,
                    x.Description,
                    x.IsActive,
                    x.CreatedAt,
                    x.CreatedByUserId
                })
                .FirstOrDefaultAsync();

            return s == null ? NotFound() : Ok(s);
        }
    }
}
