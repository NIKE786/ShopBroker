using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBroker.Data;
using ShopBroker.Models;

namespace ShopBroker.Controllers;

[Authorize(Roles = "Admin,Broker")]
public class ClientsController : Controller
{
    private readonly AppDbContext _db;
    public ClientsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q)
    {
        ViewBag.Q = q;
        var query = _db.InsuranceClients.Include(c => c.Policies).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(c => c.FullName.Contains(q) || c.Phone.Contains(q));
        return View(await query.OrderBy(c => c.FullName).ToListAsync());
    }

    public IActionResult Create() => View("Form", new InsuranceClient());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InsuranceClient m)
    {
        if (!ModelState.IsValid) return View("Form", m);
        _db.Add(m); await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var m = await _db.InsuranceClients.FindAsync(id);
        return m == null ? NotFound() : View("Form", m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, InsuranceClient m)
    {
        if (id != m.Id) return BadRequest();
        if (!ModelState.IsValid) return View("Form", m);
        _db.Update(m); await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var m = await _db.InsuranceClients.Include(c => c.Policies).ThenInclude(p => p.Company)
            .FirstOrDefaultAsync(c => c.Id == id);
        return m == null ? NotFound() : View(m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _db.Policies.AnyAsync(p => p.InsuranceClientId == id))
        { TempData["Err"] = "Is client ki policies maujood hain, delete nahi ho sakta."; return RedirectToAction(nameof(Index)); }
        var m = await _db.InsuranceClients.FindAsync(id);
        if (m != null) { _db.Remove(m); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
