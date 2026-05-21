using System;
using System.Collections.Generic;

namespace HP_Detailing.Models.ViewModels
{
    // ==========================================
    // VIEWMODEL: TicketDetailViewModel
    // MÔ TẢ: Phục vụ hiển thị trang chi tiết phiếu dịch vụ (Tickets/Detail)
    // HẬU TỐ: Sử dụng đúng hậu tố "ViewModel" theo yêu cầu
    // ==========================================
    public class TicketDetailViewModel
    {
        public int Id { get; set; }
        public string TicketCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string Plate { get; set; } = string.Empty;
        public string CarModel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AssignedStaffName { get; set; }
        public DateTime CreatedAt { get; set; }

        // Danh sách các dịch vụ hiện có trên phiếu này
        public List<TicketServiceItemViewModel> Services { get; set; } = new List<TicketServiceItemViewModel>();

        // Danh sách các dịch vụ có sẵn trong hệ thống để chọn thêm
        public List<ServiceSelectViewModel> AvailableServices { get; set; } = new List<ServiceSelectViewModel>();

        // Danh sách các vật tư xuất kho đi kèm phiếu này
        public List<TicketMaterialUsageItemViewModel> Materials { get; set; } = new List<TicketMaterialUsageItemViewModel>();

        // Tổng tiền của phiếu (tính tổng từ các dịch vụ và vật tư phụ tùng tính tiền khách)
        public decimal TotalAmount { get; set; }
    }

    public class TicketMaterialUsageItemViewModel
    {
        public int Id { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public bool IsChargedToCustomer { get; set; }
    }

    public class TicketServiceItemViewModel
    {
        public int Id { get; set; }
        public string ServiceCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ServiceSelectViewModel
    {
        public int Id { get; set; }
        public string ServiceCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
    }
}
