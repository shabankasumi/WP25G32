using System.ComponentModel.DataAnnotations;

namespace Local_Service_Manager.Models.ViewModels
{
    public class ReservationCreateVm
    {
        [Required]
        public int ServiceId { get; set; }

        public string ServiceName { get; set; } = "";

        [Required]
        [DataType(DataType.Date)]
        public DateTime ReservationDate { get; set; } = DateTime.Today.AddDays(1);

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
    }
}
