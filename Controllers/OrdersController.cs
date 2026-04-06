using System.Security.Claims;
using farm2homeWebApi.Data;
using farm2homeWebApi.DTOs;
using farm2homeWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace farm2homeWebApi.Controllers
{
    [Route("orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Danh sách voucher hợp lệ (sau này có thể chuyển vào DB)
        private readonly Dictionary<string, (decimal discount, string type, string desc)> _vouchers = new()
        {
            ["FREESHIP"] = (30000, "freeship", "Miễn phí vận chuyển"),
            ["FARM50"]   = (50000, "fixed",    "Giảm trực tiếp 50.000đ"),
        };

        private const decimal SHIPPING_FEE = 30000;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------
        // POST /orders/apply-voucher
        // Dùng cho: CheckoutPage.jsx - button "Áp dụng" voucher
        //   FE nhập code → gọi API này → hiển thị discount
        //
        // REQUEST BODY:
        // {
        //   "voucherCode": "FREESHIP",
        //   "subtotal": 255000
        // }
        //
        // RESPONSE:
        // {
        //   "isValid": true,
        //   "message": "Thành công: Đã áp dụng mã Miễn phí vận chuyển!",
        //   "discount": 30000,
        //   "discountType": "freeship"
        // }
        // ---------------------------------------------------------
        [HttpPost("apply-voucher")]
        public IActionResult ApplyVoucher([FromBody] ApplyVoucherRequest request)
        {
            var code = request.VoucherCode?.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(code))
                return Ok(new VoucherResponse
                {
                    IsValid      = false,
                    Message      = "Vui lòng nhập mã voucher.",
                    Discount     = 0,
                    DiscountType = "",
                });

            if (_vouchers.TryGetValue(code, out var voucher))
                return Ok(new VoucherResponse
                {
                    IsValid      = true,
                    Message      = $"Thành công: Đã áp dụng mã {voucher.desc}!",
                    Discount     = voucher.discount,
                    DiscountType = voucher.type,
                });

            return Ok(new VoucherResponse
            {
                IsValid      = false,
                Message      = "Mã voucher không hợp lệ hoặc đã hết lượt.",
                Discount     = 0,
                DiscountType = "",
            });
        }

        // ---------------------------------------------------------
        // POST /orders
        // Dùng cho: CheckoutPage.jsx - button "Xác Nhận Đặt Hàng"
        //   Nếu đã đăng nhập → [Authorize], dùng JWT lấy userId
        //   Nếu chưa đăng nhập → cho phép đặt với userId = null
        //
        // REQUEST BODY:
        // {
        //   "items": [
        //     { "productId": 1, "quantity": 2 },
        //     { "productId": 3, "quantity": 1 }
        //   ],
        //   "voucherCode": "FARM50",
        //   "note": "Giao buổi sáng"
        // }
        // ---------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest(new { message = "Đơn hàng phải có ít nhất 1 sản phẩm." });

            // Lấy userId từ JWT nếu đã đăng nhập (không bắt buộc)
            int? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedId))
                userId = parsedId;

            // Lấy thông tin sản phẩm từ DB để xác thực và lấy giá
            var productIds = request.Items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            // Kiểm tra tất cả sản phẩm có tồn tại không
            var notFoundIds = productIds.Except(products.Select(p => p.Id)).ToList();
            if (notFoundIds.Any())
                return BadRequest(new
                {
                    message = $"Không tìm thấy sản phẩm với ID: {string.Join(", ", notFoundIds)}"
                });

            // Kiểm tra tồn kho
            foreach (var item in request.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                if (product.Stock < item.Quantity)
                    return BadRequest(new
                    {
                        message = $"Sản phẩm '{product.Name}' chỉ còn {product.Stock} trong kho."
                    });
            }

            // Tính toán đơn hàng
            decimal discount = 0;
            string? voucherCodeUsed = null;

            if (!string.IsNullOrWhiteSpace(request.VoucherCode))
            {
                var code = request.VoucherCode.Trim().ToUpper();
                if (_vouchers.TryGetValue(code, out var voucher))
                {
                    discount = voucher.discount;
                    voucherCodeUsed = code;
                }
            }

            // Tạo Order
            var order = new Order
            {
                UserId      = userId,
                Status      = "Pending",
                ShippingFee = SHIPPING_FEE,
                Discount    = discount,
                VoucherCode = voucherCodeUsed,
                Note        = request.Note,
                CreatedAt   = DateTime.UtcNow,
            };

            // Tạo OrderItems và trừ tồn kho
            foreach (var item in request.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);

                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity  = item.Quantity,
                    UnitPrice = product.Price,   // Lưu giá tại thời điểm đặt
                });

                product.Stock -= item.Quantity; // Trừ tồn kho
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Build response
            var subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
            var total = subtotal + order.ShippingFee - order.Discount;

            var responseItems = order.Items.Select(i =>
            {
                var p = products.First(x => x.Id == i.ProductId);
                return new OrderItemDto
                {
                    ProductId    = i.ProductId,
                    ProductName  = p.Name,
                    ProductImage = p.Image,
                    UnitPrice    = i.UnitPrice,
                    Quantity     = i.Quantity,
                    Subtotal     = i.UnitPrice * i.Quantity,
                };
            }).ToList();

            return Ok(new OrderResponse
            {
                OrderId     = order.Id,
                Status      = order.Status,
                Items       = responseItems,
                Subtotal    = subtotal,
                ShippingFee = order.ShippingFee,
                Discount    = order.Discount,
                Total       = total,
                Message     = "Đặt hàng thành công! Chúng tôi sẽ liên hệ xác nhận sớm.",
                CreatedAt   = order.CreatedAt,
            });
        }

        // ---------------------------------------------------------
        // GET /orders/my
        // Dùng cho: Xem lịch sử đơn hàng của user đã đăng nhập
        //   Cần JWT Token trong header: Authorization: Bearer <token>
        // ---------------------------------------------------------
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized(new { message = "Không xác định được người dùng." });

            var orders = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var result = orders.Select(o =>
            {
                var subtotal = o.Items.Sum(i => i.UnitPrice * i.Quantity);
                return new OrderResponse
                {
                    OrderId     = o.Id,
                    Status      = o.Status,
                    Subtotal    = subtotal,
                    ShippingFee = o.ShippingFee,
                    Discount    = o.Discount,
                    Total       = subtotal + o.ShippingFee - o.Discount,
                    CreatedAt   = o.CreatedAt,
                    Items       = o.Items.Select(i => new OrderItemDto
                    {
                        ProductId    = i.ProductId,
                        ProductName  = i.Product?.Name ?? "",
                        ProductImage = i.Product?.Image ?? "",
                        UnitPrice    = i.UnitPrice,
                        Quantity     = i.Quantity,
                        Subtotal     = i.UnitPrice * i.Quantity,
                    }).ToList()
                };
            }).ToList();

            return Ok(result);
        }
    }
}
