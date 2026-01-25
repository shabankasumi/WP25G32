using System.ComponentModel.DataAnnotations;

namespace Local_Service_Manager.ViewModels
{
    public class ReservationEditVm
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int ServiceId { get; set; }

        public string ServiceName { get; set; } = "";

        [Required]
        [DataType(DataType.Date)]
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
