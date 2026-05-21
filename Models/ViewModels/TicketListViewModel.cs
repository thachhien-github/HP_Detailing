namespace HP_Detailing.Models.ViewModels
{
    public class TicketListViewModel
    {
        public IEnumerable<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
