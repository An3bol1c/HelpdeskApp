using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HelpdeskApp.Data;
using HelpdeskApp.Models;

namespace HelpdeskApp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;

    public AdminController(
        ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var tickets = await _context.Tickets
            .Where(ticket => ticket.Status != "Closed")
            .Include(ticket => ticket.Category)
            .Include(ticket => ticket.Equipment)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ToListAsync();

        return View(await TicketListViewModelFactory.CreateAsync(_context, tickets));
    }

    public async Task<IActionResult> ManageRoles()
    {
        if (string.IsNullOrEmpty(_userManager.GetUserId(User)))
        {
            return Forbid();
        }

        return View(await BuildUserRolesViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string userId, string? selectedRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var currentAdminId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(currentAdminId) || user.Id == currentAdminId)
        {
            return Forbid();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var allRoleNames = await GetRoleNamesAsync();
        var availableRoleNames = allRoleNames
            .Where(role => !currentRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var targetRole = availableRoleNames.FirstOrDefault(role =>
            role.Equals(selectedRole, StringComparison.OrdinalIgnoreCase));
        if (targetRole == null)
        {
            ModelState.AddModelError(string.Empty, "Pasirinkite vieną vartotojui dar nepriskirtą rolę.");
            return View("ManageRoles", await BuildUserRolesViewModelAsync());
        }

        var isPromotion = currentRoles.Contains("User", StringComparer.OrdinalIgnoreCase)
            && (targetRole.Equals("Technician", StringComparison.OrdinalIgnoreCase)
                || targetRole.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        await using var transaction = await _context.Database.BeginTransactionAsync();

        if (currentRoles.Count > 0)
        {
            var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeRolesResult.Succeeded)
            {
                await transaction.RollbackAsync();
                TempData["RoleError"] = string.Join(" ", removeRolesResult.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(ManageRoles));
            }
        }

        var addRoleResult = await _userManager.AddToRoleAsync(user, targetRole);
        if (!addRoleResult.Succeeded)
        {
            await transaction.RollbackAsync();
            TempData["RoleError"] = string.Join(" ", addRoleResult.Errors.Select(error => error.Description));
            return RedirectToAction(nameof(ManageRoles));
        }

        if (isPromotion)
        {
            var reportedTickets = await _context.Tickets
                .Where(ticket => ticket.ReporterId == user.Id)
                .ToListAsync();
            _context.Tickets.RemoveRange(reportedTickets);
            await _context.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        TempData["RoleMessage"] = isPromotion
            ? $"Rolė pakeista į {targetRole}, o vartotojo užregistruoti gedimai ištrinti."
            : $"Rolė pakeista į {targetRole}.";

        return RedirectToAction(nameof(ManageRoles));
    }

    private async Task<IReadOnlyList<UserRolesViewModel>> BuildUserRolesViewModelAsync()
    {
        var currentAdminId = _userManager.GetUserId(User)
            ?? throw new InvalidOperationException("The authenticated administrator has no user ID.");
        var users = await _userManager.Users
            .Where(user => user.Id != currentAdminId)
            .OrderBy(user => user.Email)
            .ToListAsync();
        var roles = await GetRoleNamesAsync();

        var userRoles = new Dictionary<string, List<string>>();
        foreach (var role in roles)
        {
            foreach (var user in await _userManager.GetUsersInRoleAsync(role))
            {
                if (!userRoles.TryGetValue(user.Id, out var assignedRoles))
                {
                    assignedRoles = [];
                    userRoles.Add(user.Id, assignedRoles);
                }

                assignedRoles.Add(role);
            }
        }

        var model = users.Select(user => new UserRolesViewModel
        {
            UserId = user.Id,
            Email = user.Email ?? user.UserName ?? user.Id,
            Roles = userRoles.TryGetValue(user.Id, out var assignedRoles)
                ? assignedRoles
                : [],
            AvailableRoles = roles
                .Where(role => !userRoles.TryGetValue(user.Id, out var userRoleNames)
                    || !userRoleNames.Contains(role, StringComparer.OrdinalIgnoreCase))
                .ToList()
        }).ToList();

        return model;
    }

    private async Task<List<string>> GetRoleNamesAsync()
    {
        return await _roleManager.Roles
            .Where(role => role.Name != null)
            .OrderBy(role => role.Name)
            .Select(role => role.Name!)
            .ToListAsync();
    }
}