using System;
using System.Collections.Generic;

namespace HP_Detailing.Models.ViewModels
{
    // ==========================================
    // VIEWMODEL: HomeDashboardViewModel
    // MÔ TẢ: Đại diện cho dữ liệu hiển thị trên trang Dashboard trang chủ (Home/Index)
    // ==========================================
    public class HomeDashboardViewModel
    {
        // ── Số liệu thống kê ──────────────────────────────────────────
        public decimal TotalRevenue { get; set; }
        public int CompletedTickets { get; set; }
        public int InProgressCars { get; set; }          // Luôn là real-time, không phụ thuộc filter
        public int TodayAppointmentsCount { get; set; }

        // ── Danh sách ────────────────────────────────────────────────
        public IEnumerable<Ticket> RecentTickets { get; set; } = new List<Ticket>();
        public IEnumerable<Appointment> TodayAppointmentsList { get; set; } = new List<Appointment>();

        // ── Trạng thái Filter đang áp dụng ───────────────────────────
        public DateTime FilterDate { get; set; } = DateTime.Today;
        public string FilterShift { get; set; } = "all";

        // Nhãn hiển thị của ca trực đang chọn
        public string ShiftLabel => FilterShift switch
        {
            "morning"   => "Ca Sáng (7:00 – 12:00)",
            "afternoon" => "Ca Chiều (12:00 – 18:00)",
            "evening"   => "Ca Tối (18:00 – 23:00)",
            _           => "Cả ngày"
        };

        // Mốc thời gian bắt đầu và kết thúc của ca đang lọc (UTC để so sánh với DB)
        public DateTime FilterFrom { get; set; }
        public DateTime FilterTo { get; set; }

        // Có đang lọc ngày hôm nay không? (dùng để hiện label "Hôm nay")
        public bool IsToday => FilterDate.Date == DateTime.Today;
    }
}

