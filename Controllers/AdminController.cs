using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBroker.Data;
using ShopBroker.Models;

namespace ShopBroker.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Orders() =>
        View(await _db.Orders.Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.OrderDate).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, string status)
    {
        var o = await _db.Orders.FindAsync(id);
        if (o != null && new[] { "Placed", "Awaiting Payment", "Paid", "Shipped", "Delivered", "Cancelled" }.Contains(status))
        { o.Status = status; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Orders));
    }

    public async Task<IActionResult> Products() => View(await _db.Products.OrderBy(p => p.Id).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProduct(int id, decimal price, int stock, bool isActive)
    {
        var p = await _db.Products.FindAsync(id);
        if (p != null && price > 0 && stock >= 0) { p.Price = price; p.Stock = stock; p.IsActive = isActive; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Products));
    }
}
