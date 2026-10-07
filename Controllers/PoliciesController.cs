using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopBroker.Data;
using ShopBroker.Models;

namespace ShopBroker.Controllers;

[Authorize(Roles = "Admin,Broker")]
public class PoliciesController : Controller
{
    private readonly AppDbContext _db;
    public PoliciesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, string? status)
    {
        ViewBag.Q = q; ViewBag.Status = status;
        var query = _db.Policies.Include(p => p.Client).Include(p => p.Company).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.PolicyNumber.Contains(q) || p.Client!.FullName.Contains(q) || p.Client!.Phone.Contains(q));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(p => p.Status == status);
        return View(await query.OrderBy(p => p.NextPremiumDue).ToListAsync());
    }

    public async Task<IActionResult> Dues(int days = 30)
    {
        ViewBag.Days = days;
        var limit = DateTime.Today.AddDays(days);
        return View(await _db.Policies.Include(p => p.Client).Include(p => p.Company)
            .Where(p => p.Status == "Active" && p.NextPremiumDue <= limit)
            .OrderBy(p => p.NextPremiumDue).ToListAsync());
    }

    public async Task<IActionResult> Create(int? clientId)
    {
        await LoadLists();
        return View("Form", new Policy
        {
            InsuranceClientId = clientId ?? 0, StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddYears(1), NextPremiumDue = DateTime.Today.AddYears(1)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Policy m)
    {
        if (!ModelState.IsValid) { await LoadLists(); return View("Form", m); }
        _db.Add(m); await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = m.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var m = await _db.Policies.FindAsync(id);
        if (m == null) return NotFound();
        await LoadLists();
        return View("Form", m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Policy m)
    {
        if (id != m.Id) return BadRequest();
        if (!ModelState.IsValid) { await LoadLists(); return View("Form", m); }
        _db.Update(m); await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var m = await _db.Policies.Include(p => p.Client).Include(p => p.Company)
            .Include(p => p.Payments).FirstOrDefaultAsync(p => p.Id == id);
        return m == null ? NotFound() : View(m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPayment(int id, decimal amount, DateTime? paidOn)
    {
        var p = await _db.Policies.FindAsync(id);
        if (p == null) return NotFound();
        if (amount <= 0) { TempData["Err"] = "Amount sahi daalein."; return RedirectToAction(nameof(Details), new { id }); }
        _db.PremiumPayments.Add(new PremiumPayment
        {
            PolicyId = id, Amount = amount, PaidOn = paidOn ?? DateTime.Today,
            CommissionEarned = Math.Round(amount * p.CommissionPercent / 100m, 2)
        });
        var months = p.PremiumFrequency switch { "Monthly" => 1, "Quarterly" => 3, "HalfYearly" => 6, _ => 12 };
        p.NextPremiumDue = p.NextPremiumDue.AddMonths(months);
        if (p.NextPremiumDue > p.EndDate && p.PremiumFrequency != "Yearly") p.NextPremiumDue = p.EndDate;
        await _db.SaveChangesAsync();
        TempData["Msg"] = "Premium payment save ho gaya.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var m = await _db.Policies.Include(p => p.Payments).FirstOrDefaultAsync(p => p.Id == id);
        if (m != null) { _db.RemoveRange(m.Payments); _db.Remove(m); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLists()
    {
        ViewBag.Clients = new SelectList(await _db.InsuranceClients.OrderBy(c => c.FullName).ToListAsync(), "Id", "FullName");
        ViewBag.Companies = new SelectList(await _db.InsuranceCompanies.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
    }
}
