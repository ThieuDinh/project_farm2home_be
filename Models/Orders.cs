namespace farm2homeWebApi.Models;

public class Order
{
    public int Id { get; set; }
    public int? UserId { get; set; }            // Nullable: cho phép khách vãng lai đặt hàng
    public AppUser? User { get; set; }

    public string Status { get; set; } = "Pending"; // Pending | Confirmed | Shipping | Done | Cancelled
    public decimal ShippingFee { get; set; } = 30000;
    public decimal Discount { get; set; } = 0;
    public string? VoucherCode { get; set; }
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }

    public int ProductId { get; set; }
    public Products Product { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }  // Lưu giá tại thời điểm đặt (phòng giá thay đổi sau)
}
