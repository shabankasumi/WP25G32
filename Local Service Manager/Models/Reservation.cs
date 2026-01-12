using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Local_Service_Manager.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        public int ServiceId { get; set; }
        public Service Service { get; set; }

        [Required]
        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        [Required]
        public DateTime ReservationDate { get; set; }

        [Required, StringLength(120)]
        public string Street { get; set; } = "";

        [Required, StringLength(60)]
        public string City { get; set; } = "";

        [Required, StringLength(15)]
        public string Zip { get; set; } = "";

        [Required, Phone, StringLength(30)]
        public string PhoneNumber { get; set; } = "";

        [StringLength(500)]
        public string? Notes { get; set; }

        public string Status { get; set; } = "Pending";
    }
}
