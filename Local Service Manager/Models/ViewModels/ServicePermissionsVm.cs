namespace Local_Service_Manager.Models.ViewModels
{
    public class ServicePermissionRowVm
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
    }

    public class ServicePermissionsVm
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public List<ServicePermissionRowVm> Users { get; set; } = new();
    }
}
