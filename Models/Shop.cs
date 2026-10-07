using System.ComponentModel.DataAnnotations;
namespace ShopBroker.Models;

public class Product
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = "";
    [StringLength(500)] public string? Description { get; set; }
    [Range(0.01, 1000000)] public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Order
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    [Required] public string CustomerName { get; set; } = "";
    [Required, Phone] public string Phone { get; set; } = "";
    [Required] public string Address { get; set; } = "";
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "COD";
    public string Status { get; set; } = "Placed";
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
