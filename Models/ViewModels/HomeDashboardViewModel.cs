using System.Collections.Generic;

namespace HP_Detailing.Models.ViewModels
{
    // ==========================================
    // VIEWMODEL: HomeDashboardViewModel
    // MÔ TẢ: Đại diện cho dữ liệu hiển thị trên trang Dashboard trang chủ (Home/Index)
    // HẬU TỐ: Sử dụng đúng hậu tố "ViewModel" theo chuẩn yêu cầu của bro
    // ==========================================
    public class HomeDashboardViewModel
    {
        // Doanh thu thực tế (Tổng tiền từ các Hóa đơn có trạng thái PAID)
        public decimal TotalRevenue { get; set; }

        // Số lượng Phiếu dịch vụ đã hoàn thành (Status = "completed")
        public int CompletedTickets { get; set; }

        // Số lượng xe đang trong quá trình thi công (Status = "in_progress")
        public int InProgressCars { get; set; }

        // Số lượng lịch đặt hẹn trong ngày hôm nay
        public int TodayAppointmentsCount { get; set; }

        // Danh sách 5 Phiếu dịch vụ tiếp nhận gần đây nhất
        public IEnumerable<Ticket> RecentTickets { get; set; } = new List<Ticket>();

        // Danh sách các Lịch đặt hẹn trong ngày hôm nay
        public IEnumerable<Appointment> TodayAppointmentsList { get; set; } = new List<Appointment>();
    }
}
