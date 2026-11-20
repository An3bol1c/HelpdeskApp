using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HelpdeskApp.Data;
using System.Security.Claims;

namespace HelpdeskApp.Controllers;

[Authorize(Roles = "User")]
public class UserController : Controller
{
    private readonly ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var reporterId = GetReporterId();
        if (reporterId == null)
        {
            return Forbid();
        }

        var recentTickets = await _context.Tickets
            .Where(ticket => ticket.ReporterId == reporterId)
            .Include(ticket => ticket.Category)
            .Include(ticket => ticket.Equipment)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .Take(5)
            .ToListAsync();

        return View(recentTickets);
    }

    public async Task<IActionResult> MyTickets()
    {
        var reporterId = GetReporterId();
        if (reporterId == null)
        {
            return Forbid();
        }

        var tickets = await _context.Tickets
            .Where(ticket => ticket.ReporterId == reporterId)
            .Include(ticket => ticket.Category)
            .Include(ticket => ticket.Equipment)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync();

        return View(tickets);
    }

    [HttpGet]
    public IActionResult CreateTicket()
    {
        return RedirectToAction("Create", "Tickets");
    }

    private string? GetReporterId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}