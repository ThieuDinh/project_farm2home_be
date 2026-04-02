using farm2homeWebApi; // Thay bằng namespace chứa AppDbContext của bạn
using farm2homeWebApi.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace farm2homeWebApi.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Kiểm tra xem Email đã bị trùng trong DB chưa
            var emailExists = _context.AppUsers.Any(u => u.Email == request.Email);
            if (emailExists)
            {
                return BadRequest(new { message = "Email này đã được sử dụng!" });
            }

            // 2. Băm mật khẩu (Hashing) - Chú ý: Chỉ băm request.Password, bỏ qua ConfirmPassword
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 3. Đổ dữ liệu từ DTO sang Entity thực tế (Chỉ gán các trường có trong form)
            var newUser = new AppUser
            {
                Email = request.Email,
                PasswordHash = hashedPassword,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,

                // Cố tình để các trường này là null, user sẽ cập nhật sau
                Province = null,
                Ward = null,
                Street = null,
                Age = null,

                IsEmailVerified = false
            };

            // 4. Lưu vào Database
            _context.AppUsers.Add(newUser);
            _context.SaveChanges();

            return Ok(new
            {
                message = "Đăng ký thành công! Vui lòng kiểm tra email để xác thực.",
                userEmail = newUser.Email
            });
        }
    }
}