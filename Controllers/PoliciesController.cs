using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ShopBroker.Data;
using ShopBroker.Helpers;
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

    public async Task<IActionResult> Dues(int days = 30, bool overdue = false)
    {
        ViewBag.Days = days; ViewBag.Overdue = overdue;
        var q = _db.Policies.Include(p => p.Client).Include(p => p.Company).Where(p => p.Status == "Active");
        q = overdue ? q.Where(p => p.NextPremiumDue < DateTime.Today)
                    : q.Where(p => p.NextPremiumDue <= DateTime.Today.AddDays(days));
        return View(await q.OrderBy(p => p.NextPremiumDue).ToListAsync());
    }

    public async Task<IActionResult> Create(int? clientId)
    {
        await LoadLists();
        var start = DateTime.Today;
        var end = start.AddMonths(12);
        return View("Form", new Policy { InsuranceClientId = clientId ?? 0, StartDate = start, EndDate = end, NextPremiumDue = end });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Policy m, bool firstPaid)
    {
        await Validate(m);
        if (!ModelState.IsValid) { await LoadLists(); return View("Form", m); }
        m.Id = 0; m.RenewalCount = 0; m.LastRenewedOn = null;
        if (firstPaid)
            m.Payments.Add(new PremiumPayment
            {
                Amount = m.PremiumAmount, PaidOn = m.StartDate, PaymentType = "New Policy",
                CommissionEarned = Math.Round(m.PremiumAmount * m.CommissionPercent / 100m, 2)
            });
        _db.Add(m);
        await _db.SaveChangesAsync();
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
        var p = await _db.Policies.FindAsync(id);
        if (p == null) return NotFound();
        m.Id = id;
        await Validate(m);
        if (!ModelState.IsValid) { m.RenewalCount = p.RenewalCount; m.LastRenewedOn = p.LastRenewedOn; await LoadLists(); return View("Form", m); }
        p.PolicyNumber = m.PolicyNumber; p.InsuranceClientId = m.InsuranceClientId; p.InsuranceCompanyId = m.InsuranceCompanyId;
        p.PolicyType = m.PolicyType; p.SumAssured = m.SumAssured; p.PremiumAmount = m.PremiumAmount;
        p.PremiumFrequency = m.PremiumFrequency; p.StartDate = m.StartDate; p.EndDate = m.EndDate;
        p.NextPremiumDue = m.NextPremiumDue; p.CommissionPercent = m.CommissionPercent; p.Status = m.Status;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var m = await _db.Policies.Include(p => p.Client).Include(p => p.Company)
            .Include(p => p.Payments).FirstOrDefaultAsync(p => p.Id == id);
        return m == null ? NotFound() : View(m);
    }

    // Renewal: naya term (start = purani end date, end = start + frequency), payment + commission record
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Renew(int id, decimal amount, DateTime? paidOn)
    {
        var p = await _db.Policies.FindAsync(id);
        if (p == null) return NotFound();
        if (amount <= 0) { TempData["Err"] = "Amount sahi daalein."; return RedirectToAction(nameof(Details), new { id }); }
        var months = PolicyHelper.Months(p.PremiumFrequency);
        p.StartDate = p.EndDate.Date;
        p.EndDate = p.StartDate.AddMonths(months);
        p.NextPremiumDue = p.EndDate;
        p.RenewalCount += 1;
        p.LastRenewedOn = paidOn ?? DateTime.Today;
        p.Status = "Active";
        _db.PremiumPayments.Add(new PremiumPayment
        {
            PolicyId = id, Amount = amount, PaidOn = paidOn ?? DateTime.Today, PaymentType = "Renewal",
            CommissionEarned = Math.Round(amount * p.CommissionPercent / 100m, 2)
        });
        await _db.SaveChangesAsync();
        TempData["Msg"] = $"Policy renew ho gayi. Naya term: {p.StartDate:dd MMM yyyy} se {p.EndDate:dd MMM yyyy}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var m = await _db.Policies.Include(p => p.Payments).FirstOrDefaultAsync(p => p.Id == id);
        if (m == null) return RedirectToAction(nameof(Index));
        if (m.RenewalCount > 0)
        {
            TempData["Err"] = "Yeh policy renew ho chuki hai, isliye delete nahi ho sakti. Zarurat ho to Status 'Cancelled' kar dein.";
            return RedirectToAction(nameof(Details), new { id });
        }
        _db.RemoveRange(m.Payments); _db.Remove(m);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task Validate(Policy m)
    {
        if (m.EndDate.Date <= m.StartDate.Date) ModelState.AddModelError(nameof(m.EndDate), "End date, start date ke baad honi chahiye.");
        if (await _db.Policies.AnyAsync(x => x.PolicyNumber == m.PolicyNumber && x.Id != m.Id))
            ModelState.AddModelError(nameof(m.PolicyNumber), "Yeh policy number pehle se maujood hai.");
    }

    private async Task LoadLists()
    {
        ViewBag.Clients = new SelectList(await _db.InsuranceClients.OrderBy(c => c.FullName).ToListAsync(), "Id", "FullName");
        ViewBag.Companies = new SelectList(await _db.InsuranceCompanies.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
    }
}
