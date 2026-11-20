using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HelpdeskApp.Data;
using HelpdeskApp.Models;

namespace HelpdeskApp.Controllers;

[Authorize(Roles = "Technician,Admin")]
public class TechnicianController : Controller
{
    private readonly ApplicationDbContext _context;

    public TechnicianController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var activeTickets = await _context.Tickets
            .Include(ticket => ticket.Category)
            .Include(ticket => ticket.Equipment)
            .Where(ticket => ticket.Status != "Closed")
            .OrderByDescending(ticket => ticket.CreatedAt)
            .Take(5)
            .ToListAsync();

        return View(await TicketListViewModelFactory.CreateAsync(_context, activeTickets));
    }

    public async Task<IActionResult> Tickets(
        string? status,
        string? search,
        string? reporterId,
        int? categoryId,
        int? equipmentId,
        string? priority,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? sortBy,
        string? sortDirection)
    {
        var selectedStatus = string.Equals(status, "closed", StringComparison.OrdinalIgnoreCase)
            ? "closed"
            : "active";
        var selectedSortBy = new[] { "title", "reporter", "priority", "category", "equipment", "created" }
            .Contains(sortBy, StringComparer.OrdinalIgnoreCase)
                ? sortBy!.ToLowerInvariant()
                : "created";
        var selectedSortDirection = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            ? "asc"
            : "desc";

        IQueryable<Ticket> filteredTickets = _context.Tickets;
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            filteredTickets = filteredTickets.Where(ticket =>
                ticket.Title.Contains(search) || ticket.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(reporterId))
        {
            filteredTickets = filteredTickets.Where(ticket => ticket.ReporterId == reporterId);
        }

        if (categoryId.HasValue)
        {
            filteredTickets = filteredTickets.Where(ticket => ticket.CategoryId == categoryId.Value);
        }

        if (equipmentId.HasValue)
        {
            filteredTickets = filteredTickets.Where(ticket => ticket.EquipmentId == equipmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            filteredTickets = filteredTickets.Where(ticket => ticket.Priority == priority);
        }

        var dateRangeValid = !fromDate.HasValue || !toDate.HasValue || fromDate.Value <= toDate.Value;
        if (!dateRangeValid)
        {
            ModelState.AddModelError(nameof(toDate), "Pabaigos data negali būti ankstesnė už pradžios datą.");
        }
        else
        {
            if (fromDate.HasValue)
            {
                var startUtc = LocalDateStartAsUtc(fromDate.Value);
                filteredTickets = filteredTickets.Where(ticket => ticket.CreatedAt >= startUtc);
            }

            if (toDate.HasValue)
            {
                var endUtc = toDate.Value == DateOnly.MaxValue
                    ? DateTime.MaxValue
                    : LocalDateStartAsUtc(toDate.Value.AddDays(1));
                filteredTickets = filteredTickets.Where(ticket => ticket.CreatedAt < endUtc);
            }
        }

        var statusCounts = await filteredTickets
            .GroupBy(ticket => ticket.Status == "Closed")
            .Select(group => new { IsClosed = group.Key, Count = group.Count() })
            .ToListAsync();
        var activeCount = statusCounts.FirstOrDefault(group => !group.IsClosed)?.Count ?? 0;
        var closedCount = statusCounts.FirstOrDefault(group => group.IsClosed)?.Count ?? 0;

        var ticketsByStatus = selectedStatus == "closed"
            ? filteredTickets.Where(ticket => ticket.Status == "Closed")
            : filteredTickets.Where(ticket => ticket.Status != "Closed");
        var orderedTickets = ApplySorting(ticketsByStatus, selectedSortBy, selectedSortDirection == "desc");
        var tickets = await orderedTickets
            .Include(ticket => ticket.Category)
            .Include(ticket => ticket.Equipment)
            .ToListAsync();

        var reporters = await _context.Users
            .OrderBy(user => user.Email)
            .Select(user => new { user.Id, user.Email, user.UserName })
            .ToListAsync();
        var categories = await _context.Categories
            .OrderBy(category => category.Name)
            .ToListAsync();
        var equipment = await _context.Equipment
            .OrderBy(item => item.Name)
            .ThenBy(item => item.InventoryNumber)
            .ToListAsync();
        var priorities = await _context.Tickets
            .Select(ticket => ticket.Priority)
            .Distinct()
            .OrderBy(value => value)
            .ToListAsync();

        var model = new TicketBrowseViewModel
        {
            Tickets = await TicketListViewModelFactory.CreateAsync(_context, tickets),
            Status = selectedStatus,
            Search = search,
            ReporterId = reporterId,
            CategoryId = categoryId,
            EquipmentId = equipmentId,
            Priority = priority,
            FromDate = fromDate,
            ToDate = toDate,
            SortBy = selectedSortBy,
            SortDirection = selectedSortDirection,
            ActiveCount = activeCount,
            ClosedCount = closedCount,
            Reporters = reporters.Select(user => new SelectListItem
            {
                Value = user.Id,
                Text = user.Email ?? user.UserName ?? user.Id,
                Selected = user.Id == reporterId
            }).ToList(),
            Categories = categories.Select(category => new SelectListItem
            {
                Value = category.Id.ToString(),
                Text = category.Name,
                Selected = category.Id == categoryId
            }).ToList(),
            Equipment = equipment.Select(item => new SelectListItem
            {
                Value = item.Id.ToString(),
                Text = $"{item.Name} ({item.InventoryNumber})",
                Selected = item.Id == equipmentId
            }).ToList(),
            Priorities = priorities.Select(value => new SelectListItem
            {
                Value = value,
                Text = value,
                Selected = value == priority
            }).ToList()
        };

        return View(model);
    }

    private IQueryable<Ticket> ApplySorting(IQueryable<Ticket> tickets, string sortBy, bool descending)
    {
        return sortBy switch
        {
            "title" => descending
                ? tickets.OrderByDescending(ticket => ticket.Title)
                : tickets.OrderBy(ticket => ticket.Title),
            "reporter" => descending
                ? tickets.OrderByDescending(ticket => _context.Users
                    .Where(user => user.Id == ticket.ReporterId)
                    .Select(user => user.Email ?? user.UserName)
                    .FirstOrDefault())
                : tickets.OrderBy(ticket => _context.Users
                    .Where(user => user.Id == ticket.ReporterId)
                    .Select(user => user.Email ?? user.UserName)
                    .FirstOrDefault()),
            "priority" => descending
                ? tickets.OrderByDescending(ticket => ticket.Priority)
                : tickets.OrderBy(ticket => ticket.Priority),
            "category" => descending
                ? tickets.OrderByDescending(ticket => ticket.Category!.Name)
                : tickets.OrderBy(ticket => ticket.Category!.Name),
            "equipment" => descending
                ? tickets.OrderByDescending(ticket => ticket.Equipment!.Name)
                : tickets.OrderBy(ticket => ticket.Equipment!.Name),
            _ => descending
                ? tickets.OrderByDescending(ticket => ticket.CreatedAt)
                : tickets.OrderBy(ticket => ticket.CreatedAt)
        };
    }

    private static DateTime LocalDateStartAsUtc(DateOnly date)
    {
        var localDate = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Local);
        return localDate.ToUniversalTime();
    }
}