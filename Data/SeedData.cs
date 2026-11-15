
using HelpdeskApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HelpdeskApp.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create roles
        foreach (var role in new[] { "User", "Technician", "Admin" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Create demonstration accounts
        await CreateUserAsync("admin@test.lt", "Admin");
        await CreateUserAsync("it@test.lt", "Technician");
        await CreateUserAsync("user@test.lt", "User");

        // Preserve the existing special Admin assignment
        var specialAdmin = await userManager.FindByEmailAsync("test1@test.lt");

        if (specialAdmin != null && !await userManager.IsInRoleAsync(specialAdmin, "Admin"))
            await userManager.AddToRoleAsync(specialAdmin, "Admin");

        // Create categories
        if (!await dbContext.Categories.AnyAsync())
        {
            dbContext.Categories.AddRange(
                new Category { Name = "Aparatinė įranga" },
                new Category { Name = "Programinė įranga" },
                new Category { Name = "Tinklas" },
                new Category { Name = "Spausdintuvai" });
        }

        // Create equipment
        if (!await dbContext.Equipment.AnyAsync())
        {
            dbContext.Equipment.AddRange(
                new Equipment { Name = "Dell Latitude 5420", InventoryNumber = "PC-001", Location = "A201 kab." },
                new Equipment { Name = "Dell Latitude 5420", InventoryNumber = "PC-002", Location = "A202 kab." },
                new Equipment { Name = "HP LaserJet Pro M404", InventoryNumber = "PR-001", Location = "Biblioteka" },
                new Equipment { Name = "Epson EB-W49 projektorius", InventoryNumber = "PJ-001", Location = "B105 aud." },
                new Equipment { Name = "TP-Link Archer AX55", InventoryNumber = "NW-001", Location = "Serverinė" });
        }

        await dbContext.SaveChangesAsync();

        async Task CreateUserAsync(string email, string role)
        {
            if (await userManager.FindByEmailAsync(email) != null)
                return;

            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, "Test123!");

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to create {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            var roleResult = await userManager.AddToRoleAsync(user, role);

            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"Failed to assign role to {email}.");
        }
    }
}
