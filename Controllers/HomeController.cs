using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBroker.Data;

namespace ShopBroker.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await _db.Products.Where(p => p.IsActive && p.Category != null)
            .Select(p => p.Category!).Distinct().OrderBy(c => c).ToListAsync();
        var featured = await _db.Products.Where(p => p.IsActive).OrderByDescending(p => p.Id).Take(8).ToListAsync();
        return View(featured);
    }
}
