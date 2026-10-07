using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBroker.Data;
using ShopBroker.Helpers;
using ShopBroker.Models;

namespace ShopBroker.Controllers;

public class ShopController : Controller
{
    private readonly AppDbContext _db;
    public ShopController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Id).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, int qty = 1)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null || !p.IsActive) return NotFound();
        if (p.Stock <= 0) { TempData["Err"] = $"{p.Name} abhi stock mein nahi hai."; return RedirectToAction(nameof(Index)); }
        var cart = CartHelper.Get(HttpContext.Session);
        var total = cart.GetValueOrDefault(id) + Math.Max(qty, 1);
        cart[id] = Math.Min(total, Math.Min(p.Stock, 99));
        CartHelper.Save(HttpContext.Session, cart);
        TempData["Msg"] = $"{p.Name} cart mein add ho gaya.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Cart() => View(await BuildLines());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult UpdateCart(Dictionary<int, int> qty)
    {
        var cart = new Dictionary<int, int>();
        foreach (var kv in qty) if (kv.Value > 0) cart[kv.Key] = Math.Min(kv.Value, 99);
        CartHelper.Save(HttpContext.Session, cart);
        return RedirectToAction(nameof(Cart));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Remove(int id)
    {
        var cart = CartHelper.Get(HttpContext.Session);
        cart.Remove(id);
        CartHelper.Save(HttpContext.Session, cart);
        return RedirectToAction(nameof(Cart));
    }

    public async Task<IActionResult> Checkout()
    {
        var lines = await BuildLines();
        if (!lines.Any()) return RedirectToAction(nameof(Index));
        return View(new CheckoutVM { Lines = lines, CustomerName = User.Identity?.IsAuthenticated == true ? User.Identity!.Name ?? "" : "" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutVM vm)
    {
        var lines = await BuildLines();
        if (!lines.Any()) return RedirectToAction(nameof(Index));
        vm.Lines = lines;
        if (!new[] { "COD", "UPI", "Card" }.Contains(vm.PaymentMethod)) vm.PaymentMethod = "COD";
        if (!ModelState.IsValid) return View(vm);

        var ids = lines.Select(l => l.ProductId).ToList();
        await using var tx = await _db.Database.BeginTransactionAsync();
        var prods = await _db.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        var order = new Order
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            CustomerName = vm.CustomerName.Trim(),
            Phone = vm.Phone.Trim(),
            Address = vm.Address.Trim(),
            PaymentMethod = vm.PaymentMethod,
            Status = vm.PaymentMethod == "COD" ? "Placed" : "Awaiting Payment"
        };
        foreach (var l in lines)
        {
            var p = prods.First(x => x.Id == l.ProductId);
            if (p.Stock < l.Qty)
            {
                ModelState.AddModelError("", $"{p.Name}: sirf {p.Stock} bacha hai.");
                return View(vm);
            }
            p.Stock -= l.Qty;
            order.Items.Add(new OrderItem { ProductId = p.Id, Quantity = l.Qty, UnitPrice = p.Price });
        }
        order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        CartHelper.Clear(HttpContext.Session);
        HttpContext.Session.SetInt32("lastOrder", order.Id);
        return RedirectToAction(nameof(Confirmation), new { id = order.Id });
    }

    public async Task<IActionResult> Confirmation(int id)
    {
        var o = await _db.Orders.Include(x => x.Items).ThenInclude(i => i.Product)
                          .FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return NotFound();
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var ok = User.IsInRole("Admin") || (uid != null && o.UserId == uid) || HttpContext.Session.GetInt32("lastOrder") == id;
        if (!ok) return Forbid();
        return View(o);
    }

    [Authorize]
    public async Task<IActionResult> MyOrders()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return View(await _db.Orders.Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.UserId == uid).OrderByDescending(o => o.OrderDate).ToListAsync());
    }

    public IActionResult Error() => Content("Kuch galat ho gaya. Kripya dobara try karein.");

    private async Task<List<CartLine>> BuildLines()
    {
        var cart = CartHelper.Get(HttpContext.Session);
        var ids = cart.Keys.ToList();
        var prods = await _db.Products.Where(p => ids.Contains(p.Id) && p.IsActive).ToListAsync();
        return prods.OrderBy(p => p.Id).Select(p => new CartLine
        {
            ProductId = p.Id, Name = p.Name, Price = p.Price, Stock = p.Stock,
            Qty = Math.Min(cart[p.Id], Math.Max(p.Stock, 0))
        }).Where(l => l.Qty > 0).ToList();
    }
}
