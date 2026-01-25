using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Local_Service_Manager.Models
{
    public class ServicePermission
    {
        public int Id { get; set; }

        [Required]
        public int ServiceId { get; set; }
        public Service? Service { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public IdentityUser? User { get; set; }

        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
    }
}
