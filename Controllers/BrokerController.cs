using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBroker.Data;
using ShopBroker.Models;

namespace ShopBroker.Controllers;

[Authorize(Roles = "Admin,Broker")]
public class BrokerController : Controller
{
    private readonly AppDbContext _db;
    public BrokerController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var active = _db.Policies.Where(p => p.Status == "Active");
        var vm = new BrokerDashboardVM
        {
            Clients = await _db.InsuranceClients.CountAsync(),
            ActivePolicies = await active.CountAsync(),
            DueIn30Days = await active.CountAsync(p => p.NextPremiumDue >= today && p.NextPremiumDue <= today.AddDays(30)),
            Overdue = await active.CountAsync(p => p.NextPremiumDue < today),
            PremiumThisMonth = await _db.PremiumPayments.Where(x => x.PaidOn >= monthStart).SumAsync(x => (decimal?)x.Amount) ?? 0,
            CommissionThisMonth = await _db.PremiumPayments.Where(x => x.PaidOn >= monthStart).SumAsync(x => (decimal?)x.CommissionEarned) ?? 0,
            CommissionTotal = await _db.PremiumPayments.SumAsync(x => (decimal?)x.CommissionEarned) ?? 0,
            Upcoming = await active.Include(p => p.Client).Include(p => p.Company)
                .Where(p => p.NextPremiumDue <= today.AddDays(30)).OrderBy(p => p.NextPremiumDue).Take(15).ToListAsync()
        };
        return View(vm);
    }

    public async Task<IActionResult> Commission(DateTime? from, DateTime? to)
    {
        var f = from ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var t = (to ?? DateTime.Today).Date;
        ViewBag.From = f; ViewBag.To = t;
        var list = await _db.PremiumPayments.Include(x => x.Policy).ThenInclude(p => p!.Client)
            .Include(x => x.Policy).ThenInclude(p => p!.Company)
            .Where(x => x.PaidOn >= f && x.PaidOn < t.AddDays(1)).OrderByDescending(x => x.PaidOn).ToListAsync();
        return View(list);
    }
}
