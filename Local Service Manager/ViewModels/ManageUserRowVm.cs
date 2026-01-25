using System.ComponentModel.DataAnnotations;

namespace Local_Service_Manager.ViewModels
{
    public class ManageUserRowVm
    {
        public string Id { get; set; } = "";

        public string? Email { get; set; }

        public string? UserName { get; set; }

        public bool EmailConfirmed { get; set; }

        public bool CanPostServices { get; set; }
    }
}
