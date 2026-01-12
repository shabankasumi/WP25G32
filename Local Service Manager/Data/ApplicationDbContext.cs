using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Local_Service_Manager.Models; // sigurohu që path-i është korrekt

namespace Local_Service_Manager.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Këtu shtohen entity-t
        public DbSet<Service> Services { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        // Opsionale: Category ose Log
        // public DbSet<Category> Categories { get; set; }
        // public DbSet<ActionLog> ActionLogs { get; set; }
    }
}
