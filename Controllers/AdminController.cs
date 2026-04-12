using farm2homeWebApi.Data;
using farm2homeWebApi.DTOs;
using farm2homeWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace farm2homeWebApi.Controllers
{
    [Authorize(Roles = "Admin")] // Cực kỳ quan trọng: Chỉ có người dùng có Role = 'Admin' trong JWT Token mới được truy cập các endpoint tại đây
    [Route("admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 0. MODULE: UPLOAD FILE
        // ==========================================
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file, [FromForm] string folder = "products")
        {
            if (file == null || file.Length == 0) return BadRequest(new { message = "Không tìm thấy file tải lên" });

            var allowedFolders = new[] { "products", "category" };
            if (!allowedFolders.Contains(folder)) folder = "products";

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", folder);
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var fileExtension = Path.GetExtension(file.FileName);
            var safeFileName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid().ToString().Substring(0,6)}{fileExtension}";
            
            var filePath = Path.Combine(uploadsPath, safeFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Trả về tên file tĩnh, frontend tự gán BASE_URL nối vô lúc hiển thị
            return Ok(new { url = safeFileName });
        }

        // ==========================================
        // 1. MODULE: QUẢN LÝ DANH MỤC (CATEGORIES)
        // ==========================================

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDto request)
        {
            var newCategory = new Categories
            {
                Name = request.Name,
                ImageUrl = request.ImageUrl,
                Description = request.Description,
                ColorCode = request.ColorCode
            };

            _context.Categories.Add(newCategory);
            await _context.SaveChangesAsync();

            request.Id = newCategory.Id; // Trả về ID vừa tạo
            return Ok(request);
        }

        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto request)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound(new { message = "Không tìm thấy danh mục" });

            category.Name = request.Name;
            category.ImageUrl = request.ImageUrl;
            category.Description = request.Description;
            category.ColorCode = request.ColorCode;

            await _context.SaveChangesAsync();
            return Ok(request);
        }

        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound(new { message = "Không tìm thấy danh mục" });

            // Kiểm tra xem có sản phẩm nào thuộc danh mục hay không
            bool hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                return BadRequest(new { message = "Không thể xóa danh mục đang chứa sản phẩm!" });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xóa danh mục thành công!" });
        }


        // ==========================================
        // 2. MODULE: QUẢN LÝ SẢN PHẨM (PRODUCTS)
        // ==========================================

        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductDto request)
        {
            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId);
            if (!categoryExists) return BadRequest(new { message = "Danh mục không tồn tại" });

            var newProduct = new Products
            {
                Name = request.Name,
                Description = request.Description,
                Image = request.Image,
                Type = request.Type,
                Price = request.Price,
                Unit = request.Unit,
                Stock = request.Stock,
                CategoryId = request.CategoryId
            };

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();
            
            request.Id = newProduct.Id;
            return Ok(request);
        }

        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto request)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm" });

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == request.CategoryId);
            if (!categoryExists) return BadRequest(new { message = "Danh mục không hợp lệ" });

            product.Name = request.Name;
            product.Description = request.Description;
            product.Image = request.Image;
            product.Type = request.Type;
            product.Price = request.Price;
            product.Unit = request.Unit;
            product.Stock = request.Stock;
            product.CategoryId = request.CategoryId;

            await _context.SaveChangesAsync();
            return Ok(request);
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm" });

            // Cân nhắc: Không xoá cứng nếu đang có đơn lưu trữ product này
            try
            {
                 _context.Products.Remove(product);
                 await _context.SaveChangesAsync();
                 return Ok(new { message = "Xóa sản phẩm thành công" });
            }
            catch(DbUpdateException)
            {
                 return BadRequest(new { message = "Không thể xóa sản phẩm này do đang có đơn hàng đính kèm!" });
            }
        }


        // ==========================================
        // 3. MODULE: QUẢN LÝ ĐƠN HÀNG (ORDERS)
        // ==========================================

        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
        {
            if (page < 1) page = 1;

            var query = _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "Tất cả")
            {
                query = query.Where(o => o.Status == status);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderResponse
                {
                    OrderId = o.Id,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt,
                    Subtotal = o.Items.Sum(i => i.UnitPrice * i.Quantity),
                    ShippingFee = o.ShippingFee,
                    Discount = o.Discount,
                    Total = o.Items.Sum(i => i.UnitPrice * i.Quantity) + o.ShippingFee - o.Discount,
                    Message = o.Note ?? ""
                })
                .ToListAsync();

            return Ok(new { items, totalCount, page, pageSize });
        }

        [HttpGet("orders/{id}")]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound(new { message = "Đơn hàng không tồn tại" });

            var response = new OrderResponse
            {
                OrderId = order.Id,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                Subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity),
                ShippingFee = order.ShippingFee,
                Discount = order.Discount,
                Total = order.Items.Sum(i => i.UnitPrice * i.Quantity) + order.ShippingFee - order.Discount,
                Message = order.Note ?? "",
                Items = order.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    ProductImage = i.Product.Image,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    Subtotal = i.UnitPrice * i.Quantity
                }).ToList()
            };

            // Thêm field User cho dễ quan sát
            var finalObj = new 
            {
                Order = response,
                CustomerInfo = order.User != null ? new { order.User.Email, order.User.FullName, order.User.PhoneNumber, order.User.Province, order.User.Ward, order.User.Street } : null
            };

            return Ok(finalObj);
        }

        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound(new { message = "Đơn hàng không tồn tại" });

            order.Status = request.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật trạng thái thành công" });
        }


        // ==========================================
        // 4. MODULE: QUẢN LÝ NGƯỜI DÙNG (USERS)
        // ==========================================

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.AppUsers
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new AdminUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    Role = u.Role,
                    IsBanned = u.IsBanned,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var emailExists = await _context.AppUsers.AnyAsync(u => u.Email == request.Email);
            if (emailExists) return BadRequest(new { message = "Email này đã được sử dụng!" });

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var newUser = new AppUser
            {
                Email = request.Email,
                PasswordHash = hashedPassword,
                FullName = request.FullName,
                Role = request.Role,
                IsEmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.AppUsers.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã tạo tài khoản thành công!", userId = newUser.Id });
        }

        [HttpPut("users/{id}/password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã đặt lại mật khẩu cho [{user.Email}] thành công!" });
        }

        [HttpPut("users/{id}/ban")]
        public async Task<IActionResult> ToggleBan(int id, [FromBody] ToggleBanRequest request)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null) return NotFound(new { message = "Người dùng không tồn tại" });

            // Không cho phép tự ban chính mình
            var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (adminId == id.ToString())
            {
                return BadRequest(new { message = "Bạn không thể tự khóa tài khoản của chính mình!" });
            }

            user.IsBanned = request.IsBanned;
            await _context.SaveChangesAsync();

            string statusText = user.IsBanned ? "Khóa" : "Mở khóa";
            return Ok(new { message = $"Đã {statusText} tài khoản [{user.Email}] thành công!" });
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleRequest request)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null) return NotFound(new { message = "Người dùng không tồn tại" });
            
            // Validation
            if (request.Role != "Admin" && request.Role != "Customer")
            {
                return BadRequest(new { message = "Role không hợp lệ. Chỉ chấp nhận 'Admin' hoặc 'Customer'"});
            }

            user.Role = request.Role;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã thay đổi quyền hạn của [{user.Email}] thành {user.Role}" });
        }
        
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.AppUsers.FindAsync(id);
            if (user == null) return NotFound(new { message = "Người dùng không tồn tại" });

            // Không cho phép tự xóa chính mình
            var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (adminId == id.ToString())
            {
                return BadRequest(new { message = "Bạn không thể tự xóa tài khoản của chính mình!" });
            }

            try
            {
                _context.AppUsers.Remove(user);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã xóa người dùng khỏi hệ thống" });
            }
            catch(DbUpdateException)
            {
                return BadRequest(new { message = "Không thể xóa do user này đang có đơn hàng trong hệ thống!"});
            }
        }
    }
}
