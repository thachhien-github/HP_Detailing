using HP_Detailing.Models;

namespace HP_Detailing.Models.ViewModels
{
    public class SettingsViewModel
    {
        public List<UserAccountViewModel> Accounts { get; set; } = new List<UserAccountViewModel>();
        public List<string> AvailableRoles { get; set; } = new List<string>();
        public List<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
    }

    public class UserAccountViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public string AvatarUrl { get; set; }
    }

    public class CreateOrUpdateUserModel
    {
        public string Id { get; set; } // Nếu rỗng là tạo mới
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
}
