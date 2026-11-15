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
        var tickets = await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.Equipment)
            .ToListAsync();

        return View(tickets);
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

        if (!await ValidateTicketAsync(title, categoryId, equipmentId))
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Equipment = await _context.Equipment.ToListAsync();

            return View();
        }

        title = title.Trim();

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


    // Display details of a specific ticket
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Category)
            .Include(t => t.Equipment)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        return View(ticket);
    }


    private async Task<bool> ValidateTicketAsync(
    string title,
    int categoryId,
    int equipmentId)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 150)
        {
            ModelState.AddModelError("Title",
                "Pavadinimas turi būti nuo 1 iki 150 simbolių.");
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
}

