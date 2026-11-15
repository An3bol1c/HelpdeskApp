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

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
    string title,
    string description,
    int categoryId,
    int equipmentId)
    {
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
}