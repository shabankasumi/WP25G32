using System.ComponentModel.DataAnnotations;

namespace Local_Service_Manager.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = "";

        [Required]
        [StringLength(60)]
        public string Category { get; set; } = "";

        [StringLength(300)]
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }

        public string CreatedByUserId { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

}
