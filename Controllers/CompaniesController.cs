using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBroker.Data;
using ShopBroker.Models;

namespace ShopBroker.Controllers;

[Authorize(Roles = "Admin,Broker")]
public class CompaniesController : Controller
{
    private readonly AppDbContext _db;
    public CompaniesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.InsuranceCompanies.OrderBy(c => c.Name).ToListAsync());
    public IActionResult Create() => View("Form", new InsuranceCompany());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InsuranceCompany m)
    {
        if (!ModelState.IsValid) return View("Form", m);
        _db.Add(m); await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var m = await _db.InsuranceCompanies.FindAsync(id);
        return m == null ? NotFound() : View("Form", m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InsuranceCompany m)
    {
        if (id != m.Id) return BadRequest();
        if (!ModelState.IsValid) return View("Form", m);
        _db.Update(m); await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _db.Policies.AnyAsync(p => p.InsuranceCompanyId == id))
        { TempData["Err"] = "Is company ki policies maujood hain, delete nahi ho sakti."; return RedirectToAction(nameof(Index)); }
        var m = await _db.InsuranceCompanies.FindAsync(id);
        if (m != null) { _db.Remove(m); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
