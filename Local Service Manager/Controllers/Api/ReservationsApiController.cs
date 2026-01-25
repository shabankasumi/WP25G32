using Local_Service_Manager.Data;
using Local_Service_Manager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Local_Service_Manager.Controllers.Api
{
    [Route("api/reservations")]
    [ApiController]
    public class ReservationsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReservationsApiController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        [HttpGet("mine")]
        public async Task<IActionResult> Mine()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var items = await _context.Reservations
                .Include(r => r.Service)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.ReservationDate)
                .Select(r => new
                {
                    r.Id,
                    r.ServiceId,
                    ServiceName = r.Service!.Name,
                    r.ReservationDate,
                    r.Street,
                    r.City,
                    r.Zip,
                    r.PhoneNumber,
                    r.Notes,
                    r.Status
                })
                .ToListAsync();

            return Ok(items);
        }

        public record CreateReservationRequest(
            int ServiceId,
            DateTime ReservationDate,
            string? Street,
            string? City,
            string? Zip,
            string? PhoneNumber,
            string? Notes
        );

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReservationRequest req)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == req.ServiceId && s.IsActive);
            if (service == null) return BadRequest(new { error = "Invalid service." });

            if (req.ReservationDate.Date < DateTime.Today)
                return BadRequest(new { error = "ReservationDate must be today or in the future." });

            var reservation = new Reservation
            {
                ServiceId = req.ServiceId,
                UserId = userId,
                ReservationDate = req.ReservationDate,
                Street = req.Street,
                City = req.City,
                Zip = req.Zip,
                PhoneNumber = req.PhoneNumber,
                Notes = req.Notes,
                Status = "Pending"
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Mine), new { id = reservation.Id }, new { reservation.Id });
        }
    }
}
