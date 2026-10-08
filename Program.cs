using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopBroker.Data;
using ShopBroker.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddDefaultIdentity<IdentityUser>(o => o.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(60);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Shop/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Shop}/{action=Index}/{id?}");
app.MapRazorPages();

// ---- Database + seed data ----
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var r in new[] { "Admin", "Broker", "Customer" })
        if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));

    var users = sp.GetRequiredService<UserManager<IdentityUser>>();
    async Task Seed(string email, string pwd, string role)
    {
        if (await users.FindByEmailAsync(email) != null) return;
        var u = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        if ((await users.CreateAsync(u, pwd)).Succeeded) await users.AddToRoleAsync(u, role);
    }
    await Seed("admin@shop.com", "Admin@123", "Admin");
    await Seed("broker@shop.com", "Broker@123", "Broker");

    if (!db.InsuranceCompanies.Any())
    {
        db.InsuranceCompanies.AddRange(
            new InsuranceCompany { Name = "LIC of India India" },
            new InsuranceCompany { Name = "HDFC Life" },
            new InsuranceCompany { Name = "ICICI Prudential" },
            new InsuranceCompany { Name = "Star Health" },
            new InsuranceCompany { Name = "Bajaj Allianz" });
        await db.SaveChangesAsync();
    }
}
app.Run();
