using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopBroker.Models;
namespace ShopBroker.Data;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> o) : base(o) { }
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<InsuranceCompany> InsuranceCompanies => Set<InsuranceCompany>();
    public DbSet<InsuranceClient> InsuranceClients => Set<InsuranceClient>();
    public DbSet<Policy> Policies => Set<Policy>();
    public DbSet<PremiumPayment> PremiumPayments => Set<PremiumPayment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        foreach (var p in b.Model.GetEntityTypes().SelectMany(t => t.GetProperties())
                 .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            p.SetPrecision(18);
        b.Entity<Policy>().HasIndex(p => p.PolicyNumber).IsUnique();

        var names = new[] { "Rice 5kg","Wheat Flour 5kg","Cooking Oil 1L","Sugar 1kg","Tea 500g",
                            "Salt 1kg","Lentils 1kg","Spice Mix","Biscuits Pack","Soap Pack" };
        var prices = new[] { 350m,220m,160m,48m,210m,22m,130m,95m,40m,120m };
        b.Entity<Product>().HasData(names.Select((n, i) => new Product
            { Id = i + 1, Name = n, Price = prices[i], Stock = 100, Description = n }));
    }
}
