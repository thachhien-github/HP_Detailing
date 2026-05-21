namespace HP_Detailing.Models.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int CompletedTickets { get; set; }
        public int InProgressCars { get; set; }
        public int TodayAppointments { get; set; }

        public IEnumerable<Ticket> RecentTickets { get; set; } = new List<Ticket>();
    }
}
