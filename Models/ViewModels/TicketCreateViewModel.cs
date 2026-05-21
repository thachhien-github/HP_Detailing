using System.Collections.Generic;

namespace HP_Detailing.Models.ViewModels
{
    public class TicketCreateViewModel
    {
        public List<ServiceCreateItemViewModel> Services { get; set; } = new();
        public List<MaterialCreateItemViewModel> Materials { get; set; } = new();
        public List<StaffCreateItemViewModel> Staffs { get; set; } = new();

        // Thừa hưởng dữ liệu điền sẵn từ Lịch hẹn (nếu có)
        public int? AppointmentId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? Plate { get; set; }
        public string? SuggestedServices { get; set; }
        public int? AssignedStaffId { get; set; }
        /// <summary>ID dịch vụ gợi ý từ lịch hẹn (khớp tên/mã trong Appointment.Services).</summary>
        public List<int> PreselectedServiceIds { get; set; } = new();
    }

    public class ServiceCreateItemViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public decimal Price { get; set; }
        public List<ServiceMaterialCreateViewModel> Materials { get; set; } = new();
    }

    public class ServiceMaterialCreateViewModel
    {
        public int MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public decimal DefaultQty { get; set; }
    }

    public class MaterialCreateItemViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Stock { get; set; }
    }

    public class StaffCreateItemViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
    }
}
