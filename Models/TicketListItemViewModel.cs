namespace HelpdeskApp.Models;

public class TicketListItemViewModel
{
    public Ticket Ticket { get; set; } = null!;
    public string ReporterEmail { get; set; } = "Unknown";
}
