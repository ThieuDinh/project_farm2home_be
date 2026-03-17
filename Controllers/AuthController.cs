using farm2homeWebApi; // Thay bằng namespace chứa AppDbContext của bạn
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
            // 1. Kiểm tra xem Email đã bị trùng trong DB chưa
            var emailExists = _context.AppUsers.Any(u => u.Email == request.Email);
            if (emailExists)
            {
                return BadRequest(new { message = "Email này đã được sử dụng!" });
            }

            // 2. Băm mật khẩu (Hashing) - Tuyệt đối không lưu request.Password
            // BCrypt sẽ tự động sinh "muối" (salt) để bảo vệ mật khẩu
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 3. Đổ dữ liệu từ DTO sang Entity thực tế
            var newUser = new AppUser
            {
                Email = request.Email,
                PasswordHash = hashedPassword,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Province = request.Province,
                Ward = request.Ward,
                Street = request.Street,
                Age = request.Age,
                IsEmailVerified = false // Mặc định là chưa xác thực email
            };

            // 4. Lưu vào Database
            _context.AppUsers.Add(newUser);
            _context.SaveChanges();

            // Trả về thông báo thành công (KHÔNG trả về PasswordHash)
            return Ok(new { 
                message = "Đăng ký thành công! Vui lòng kiểm tra email để xác thực.",
                userEmail = newUser.Email 
            });
        }
    }
}