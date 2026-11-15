using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HelpdeskApp.Data;
using HelpdeskApp.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    Console.WriteLine("SEED PRADETAS");
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    foreach (var role in new[] { "User", "Technician", "Admin" })
        if (!await roleMgr.RoleExistsAsync(role))
            await roleMgr.CreateAsync(new IdentityRole(role));

    async Task Seed(string email, string role)
    {
        if (await userMgr.FindByEmailAsync(email) != null) return;
        var u = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userMgr.CreateAsync(u, "Test123!");
        if (!result.Succeeded)
        {
            Console.WriteLine("SEED KLAIDA: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }
        await userMgr.AddToRoleAsync(u, role);
    }

    await Seed("admin@test.lt", "Admin");
    await Seed("it@test.lt", "Technician");
    await Seed("user@test.lt", "User");

    Console.WriteLine("SEED BAIGTAS. Naudotoju DB: " + userMgr.Users.Count());
}

using (var scope2 = app.Services.CreateScope())
{
    var um = scope2.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var u = await um.FindByEmailAsync("test1@test.lt");
    if (u != null && !await um.IsInRoleAsync(u, "Admin"))
        await um.AddToRoleAsync(u, "Admin");
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!db.Categories.Any())
    {
        db.Categories.AddRange(
            new Category { Name = "Aparatinė įranga" },
            new Category { Name = "Programinė įranga" },
            new Category { Name = "Tinklas" },
            new Category { Name = "Spausdintuvai" });
        db.SaveChanges();
    }

    if (!db.Equipment.Any())
    {
        db.Equipment.AddRange(
            new Equipment { Name = "Dell Latitude 5420", InventoryNumber = "PC-001", Location = "A201 kab." },
            new Equipment { Name = "Dell Latitude 5420", InventoryNumber = "PC-002", Location = "A202 kab." },
            new Equipment { Name = "HP LaserJet Pro M404", InventoryNumber = "PR-001", Location = "Biblioteka" },
            new Equipment { Name = "Epson EB-W49 projektorius", InventoryNumber = "PJ-001", Location = "B105 aud." },
            new Equipment { Name = "TP-Link Archer AX55", InventoryNumber = "NW-001", Location = "Serverinė" });
        db.SaveChanges();
    }
}

app.Run();