using HelpdeskApp.Data;
using HelpdeskApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpdeskApp.Controllers;

[Authorize(Roles = "Technician,Admin")]
public class EquipmentController : Controller
{
    private readonly ApplicationDbContext _context;

    public EquipmentController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Equipment());
    }

    public async Task<IActionResult> Index()
    {
        var equipment = await _context.Equipment
            .OrderBy(item => item.Name)
            .ThenBy(item => item.InventoryNumber)
            .ToListAsync();

        return View(equipment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,InventoryNumber,Location")] Equipment equipment)
    {
        equipment.Name = equipment.Name?.Trim() ?? string.Empty;
        equipment.InventoryNumber = equipment.InventoryNumber?.Trim() ?? string.Empty;
        equipment.Location = equipment.Location?.Trim() ?? string.Empty;

        ModelState.Clear();
        TryValidateModel(equipment);

        if (string.IsNullOrWhiteSpace(equipment.Name))
        {
            ModelState.AddModelError(nameof(Equipment.Name), "Įrenginio pavadinimas privalomas.");
        }

        if (string.IsNullOrWhiteSpace(equipment.InventoryNumber))
        {
            ModelState.AddModelError(nameof(Equipment.InventoryNumber), "Inventoriaus numeris privalomas.");
        }

        if (string.IsNullOrWhiteSpace(equipment.Location))
        {
            ModelState.AddModelError(nameof(Equipment.Location), "Vieta privaloma.");
        }

        if (!ModelState.IsValid)
        {
            return View(equipment);
        }

        _context.Equipment.Add(equipment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Įrenginys sėkmingai pridėtas.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var equipment = await _context.Equipment.FindAsync(id);
        if (equipment == null)
        {
            return NotFound();
        }

        return View(new EquipmentEditViewModel
        {
            Id = equipment.Id,
            Name = equipment.Name,
            Location = equipment.Location,
            InventoryNumber = equipment.InventoryNumber
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EquipmentEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var equipment = await _context.Equipment.FindAsync(id);
        if (equipment == null)
        {
            return NotFound();
        }

        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Location = model.Location?.Trim() ?? string.Empty;
        model.InventoryNumber = equipment.InventoryNumber;

        ModelState.Clear();
        TryValidateModel(model);

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Įrenginio pavadinimas privalomas.");
        }

        if (string.IsNullOrWhiteSpace(model.Location))
        {
            ModelState.AddModelError(nameof(model.Location), "Vieta privaloma.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        equipment.Name = model.Name;
        equipment.Location = model.Location;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Įrenginio duomenys atnaujinti.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var equipment = await _context.Equipment.FindAsync(id);
        if (equipment == null)
        {
            return NotFound();
        }

        ViewBag.RelatedTicketCount = await _context.Tickets
            .CountAsync(ticket => ticket.EquipmentId == id);
        return View(equipment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var equipment = await _context.Equipment.FindAsync(id);
        if (equipment == null)
        {
            return NotFound();
        }

        var relatedTickets = await _context.Tickets
            .Where(ticket => ticket.EquipmentId == id)
            .ToListAsync();
        _context.Tickets.RemoveRange(relatedTickets);
        _context.Equipment.Remove(equipment);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Įrenginys ir susiję gedimai ({relatedTickets.Count}) ištrinti.";
        return RedirectToAction(nameof(Index));
    }
}
