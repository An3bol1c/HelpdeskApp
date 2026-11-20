using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HelpdeskApp.Data;
using HelpdeskApp.Models;
using System.Security.Claims;

namespace HelpdeskApp.Controllers;

[Authorize]
public class TicketsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TicketsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Display all tickets
    public async Task<IActionResult> Index()
    {
        var visibleTickets = GetVisibleTickets();
        if (visibleTickets == null)
        {
            return Forbid();
        }

        if (User.IsInRole("Admin") || User.IsInRole("Technician"))
        {
            visibleTickets = visibleTickets.Where(ticket => ticket.Status != "Closed");
        }

        var tickets = await visibleTickets
            .Include(t => t.Category)
            .Include(t => t.Equipment)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return View(await TicketListViewModelFactory.CreateAsync(_context, tickets));
    }

    // Display the ticket creation form
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.Equipment = await _context.Equipment.ToListAsync();

        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string title,
        string description,
        int categoryId,
        int equipmentId)
    {
        title = title?.Trim() ?? string.Empty;
        description ??= string.Empty;

        if (!await ValidateTicketAsync(title, description, categoryId, equipmentId))
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Equipment = await _context.Equipment.ToListAsync();

            return View();
        }

        var ticket = new Ticket
        {
            Title = title,
            Description = description,
            CategoryId = categoryId,
            EquipmentId = equipmentId,
            ReporterId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Technician,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int id, string? returnUrl = null)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound();
        }

        if (!string.Equals(ticket.Status, "Closed", StringComparison.OrdinalIgnoreCase))
        {
            ticket.Status = "Closed";
            ticket.ClosedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        if (Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl!);
        }

        return RedirectToAction("Index", User.IsInRole("Admin") ? "Admin" : "Technician");
    }


    // Display details of a specific ticket
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var visibleTickets = GetVisibleTickets();
        if (visibleTickets == null)
        {
            return Forbid();
        }

        var ticket = await visibleTickets
            .Include(t => t.Category)
            .Include(t => t.Equipment)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        if (User.IsInRole("Admin") || User.IsInRole("Technician"))
        {
            ViewData["ReporterEmail"] = await _context.Users
                .Where(user => user.Id == ticket.ReporterId)
                .Select(user => user.Email ?? user.UserName)
                .FirstOrDefaultAsync() ?? "Unknown";
        }

        return View(ticket);
    }


    private async Task<bool> ValidateTicketAsync(
    string title,
    string description,
    int categoryId,
    int equipmentId)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 50)
        {
            ModelState.AddModelError("Title",
                "Pavadinimas turi būti nuo 1 iki 50 simbolių.");
        }

        if (description.Length > 500)
        {
            ModelState.AddModelError("Description",
                "Aprašymas negali viršyti 500 simbolių.");
        }

        if (!await _context.Categories.AnyAsync(c => c.Id == categoryId))
        {
            ModelState.AddModelError("CategoryId",
                "Pasirinkta kategorija neegzistuoja.");
        }

        if (!await _context.Equipment.AnyAsync(e => e.Id == equipmentId))
        {
            ModelState.AddModelError("EquipmentId",
                "Pasirinkta įranga neegzistuoja.");
        }

        return ModelState.IsValid;
    }

    private IQueryable<Ticket>? GetVisibleTickets()
    {
        if (User.IsInRole("Admin") || User.IsInRole("Technician"))
        {
            return _context.Tickets;
        }

        var reporterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return reporterId == null
            ? null
            : _context.Tickets.Where(ticket => ticket.ReporterId == reporterId);
    }
}

