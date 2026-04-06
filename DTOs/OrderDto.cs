namespace farm2homeWebApi.DTOs;

// Một item trong giỏ hàng khi checkout
public class OrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

// Request gửi lên khi nhấn "Xác nhận đặt hàng" - CheckoutPage.jsx
public class CreateOrderRequest
{
    public List<OrderItemRequest> Items { get; set; }
    public string? VoucherCode { get; set; }    // VD: "FREESHIP", "FARM50"
    public string? Note { get; set; }           // Ghi chú đơn hàng (nếu FE có)
}

// Request kiểm tra voucher - CheckoutPage.jsx button "Áp dụng"
public class ApplyVoucherRequest
{
    public string VoucherCode { get; set; }
    public decimal Subtotal { get; set; }       // Tạm tính để tính discount chính xác
}

// Response khi áp dụng voucher
public class VoucherResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; }
    public decimal Discount { get; set; }       // Số tiền được giảm
    public string DiscountType { get; set; }    // "fixed" | "freeship" | "percent"
}

// Một item trong đơn hàng (response)
public class OrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string ProductImage { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
}

// Response sau khi đặt hàng thành công
public class OrderResponse
{
    public int OrderId { get; set; }
    public string Status { get; set; }
    public List<OrderItemDto> Items { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
}
