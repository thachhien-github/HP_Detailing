using System;
using System.Collections.Generic;

namespace HP_Detailing.Models.ViewModels
{
    // ==========================================
    // VIEWMODEL: TicketIndexViewModel
    // MÔ TẢ: Phục vụ hiển thị danh sách phiếu dịch vụ (Tickets/Index)
    // HẬU TỐ: Sử dụng đúng hậu tố "ViewModel" theo yêu cầu
    // ==========================================
    public class TicketIndexViewModel
    {
        // Danh sách các phiếu dịch vụ đã nạp
        public IEnumerable<TicketItemViewModel> Tickets { get; set; } = new List<TicketItemViewModel>();
    }

    public class TicketItemViewModel
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
        
        // Tổng số tiền dịch vụ trên phiếu này (được tính động từ SQL)
        public decimal TotalPrice { get; set; }
    }
}
