using HelpdeskApp.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpdeskApp.Data;

public static class TicketListViewModelFactory
{
    public static async Task<IReadOnlyList<TicketListItemViewModel>> CreateAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<Ticket> tickets)
    {
        var reporterIds = tickets
            .Where(ticket => !string.IsNullOrEmpty(ticket.ReporterId))
            .Select(ticket => ticket.ReporterId!)
            .Distinct()
            .ToArray();

        var reporterEmails = await context.Users
            .Where(user => reporterIds.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => user.Email ?? user.UserName ?? "Unknown");

        return tickets.Select(ticket => new TicketListItemViewModel
        {
            Ticket = ticket,
            ReporterEmail = ticket.ReporterId != null
                && reporterEmails.TryGetValue(ticket.ReporterId, out var email)
                    ? email
                    : "Unknown"
        }).ToList();
    }
}
