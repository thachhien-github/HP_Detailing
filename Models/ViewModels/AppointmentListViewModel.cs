namespace HP_Detailing.Models.ViewModels
{
    public class AppointmentListViewModel
    {
        public IEnumerable<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
