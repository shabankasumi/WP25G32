using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Local_Service_Manager.Models
{
    /// <summary>
    /// User-level permissions that Admin can grant.
    /// NOTE: This is intentionally not a role (project requirement: only Admin and User roles).
    /// </summary>
    public class UserPermission
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = "";

        public IdentityUser? User { get; set; }

        /// <summary>
        /// If true, the user can create services (be a "worker/provider").
        /// </summary>
        public bool CanPostServices { get; set; }
    }
}
