using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HelpdeskApp.Data;

namespace HelpdeskApp.Controllers;

[Authorize]
public class TicketsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TicketsController(ApplicationDbContext context)
    {
        _context = context;
    }

public async Task<IActionResult> Index()
{
    var tickets = await _context.Tickets
        .Include(t => t.Category)
        .Include(t => t.Equipment)
        .ToListAsync();

    return View(tickets);
}
}