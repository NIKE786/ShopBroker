using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBroker.Data;
using ShopBroker.Helpers;
using ShopBroker.Models;

namespace ShopBroker.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    public AdminController(AppDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

    // ---------------- ORDERS ----------------
    public async Task<IActionResult> Orders(string? status)
    {
        ViewBag.Status = status;
        var q = _db.Orders.Include(o => o.Items).ThenInclude(i => i.Product).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(o => o.Status == status);
        return View(await q.OrderByDescending(o => o.OrderDate).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, string status)
    {
        var o = await _db.Orders.FindAsync(id);
        if (o != null && OrderHelper.Statuses.Contains(status)) { o.Status = status; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Orders));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPaymentLink(int id, string? link)
    {
        var o = await _db.Orders.FindAsync(id);
        if (o == null) return NotFound();
        link = link?.Trim();
        if (string.IsNullOrEmpty(link)) o.PaymentLink = null;
        else if (Uri.TryCreate(link, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp))
        {
            o.PaymentLink = u.ToString();
            if (o.Status is "Placed" or "Awaiting Payment") o.Status = "Payment Link Sent";
        }
        else { TempData["Err"] = "Sahi http/https payment link daalein."; return RedirectToAction(nameof(Orders)); }
        await _db.SaveChangesAsync();
        TempData["Msg"] = $"Order #{id} ka payment link save ho gaya.";
        return RedirectToAction(nameof(Orders));
    }

    // ---------------- PRODUCTS (Add / Edit / Delete) ----------------
    public async Task<IActionResult> Products() => View(await _db.Products.OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync());

    public IActionResult ProductCreate() => View("ProductForm", new Product { Stock = 999 });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductCreate(Product m, IFormFile? image)
    {
        var url = await SaveImage(image);
        if (!ModelState.IsValid) return View("ProductForm", m);
        m.Id = 0; m.ImageUrl = url;
        _db.Products.Add(m);
        await _db.SaveChangesAsync();
        TempData["Msg"] = "Product add ho gaya.";
        return RedirectToAction(nameof(Products));
    }

    public async Task<IActionResult> ProductEdit(int id)
    {
        var m = await _db.Products.FindAsync(id);
        return m == null ? NotFound() : View("ProductForm", m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductEdit(int id, Product m, IFormFile? image, bool removeImage)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return NotFound();
        var url = await SaveImage(image);
        if (!ModelState.IsValid) { m.ImageUrl = p.ImageUrl; return View("ProductForm", m); }
        p.Name = m.Name; p.Category = m.Category; p.Description = m.Description;
        p.Price = m.Price; p.Stock = m.Stock; p.IsActive = m.IsActive;
        if (url != null) { DeleteImageFile(p.ImageUrl); p.ImageUrl = url; }
        else if (removeImage) { DeleteImageFile(p.ImageUrl); p.ImageUrl = null; }
        await _db.SaveChangesAsync();
        TempData["Msg"] = "Product update ho gaya.";
        return RedirectToAction(nameof(Products));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductDelete(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return RedirectToAction(nameof(Products));
        if (await _db.OrderItems.AnyAsync(i => i.ProductId == id))
        {
            // Purane orders mein use hua hai - history bachane ke liye sirf hide karte hain.
            p.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Is product ke orders maujood hain, isliye isse hide (inactive) kar diya gaya.";
        }
        else
        {
            DeleteImageFile(p.ImageUrl);
            _db.Products.Remove(p);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Product delete ho gaya.";
        }
        return RedirectToAction(nameof(Products));
    }

    private async Task<string?> SaveImage(IFormFile? f)
    {
        if (f == null || f.Length == 0) return null;
        var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext) || f.Length > 2 * 1024 * 1024)
        {
            ModelState.AddModelError("", "Image sirf jpg / png / webp aur 2 MB tak ki ho sakti hai.");
            return null;
        }
        var dir = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(dir);
        var name = Guid.NewGuid().ToString("N") + ext;
        await using var fs = System.IO.File.Create(Path.Combine(dir, name));
        await f.CopyToAsync(fs);
        return "/uploads/products/" + name;
    }

    private void DeleteImageFile(string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith("/uploads/products/")) return;
        var path = Path.Combine(_env.WebRootPath, "uploads", "products", Path.GetFileName(url));
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }
}
