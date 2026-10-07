using System.ComponentModel.DataAnnotations;
namespace ShopBroker.Models;

public class CartLine
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Qty { get; set; }
    public int Stock { get; set; }
    public decimal Total => Price * Qty;
}

public class CheckoutVM
{
    [Required, Display(Name = "Full name"), StringLength(100)] public string CustomerName { get; set; } = "";
    [Required, Phone, Display(Name = "Mobile number")] public string Phone { get; set; } = "";
    [Required, Display(Name = "Delivery address"), StringLength(300)] public string Address { get; set; } = "";
    public string PaymentMethod { get; set; } = "COD";
    public List<CartLine> Lines { get; set; } = new();
    public decimal Total => Lines.Sum(l => l.Total);
}

public class BrokerDashboardVM
{
    public int Clients { get; set; }
    public int ActivePolicies { get; set; }
    public int DueIn30Days { get; set; }
    public int Overdue { get; set; }
    public decimal PremiumThisMonth { get; set; }
    public decimal CommissionThisMonth { get; set; }
    public decimal CommissionTotal { get; set; }
    public List<Policy> Upcoming { get; set; } = new();
}
