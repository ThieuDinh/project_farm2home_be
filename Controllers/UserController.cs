using System.Security.Claims;
using BCrypt.Net;
using farm2homeWebApi.Data;
using farm2homeWebApi.Models;
using farm2homeWebApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace farm2homeWebApi.Controllers
{
    [Route("user")]
    [ApiController]
    [Authorize] // BẮT BUỘC: Chỉ những request có mang Token JWT hợp lệ mới được vào đây
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // --- HÀM HỖ TRỢ LẤY ID NGƯỜI DÙNG TỪ TOKEN ---
        private int GetCurrentUserId()
        {
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(
                    System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub
                )?.Value;

            return int.Parse(userIdClaim);
        }

        // 1. LẤY THÔNG TIN HỒ SƠ (GET /user/profile)
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            int userId = GetCurrentUserId();
            var user = _context.AppUsers.FirstOrDefault(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            var response = new UserProfileResponse
            {
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
               
                Province = user.Province,
                Ward = user.Ward,
                Street = user.Street,
                // Trả về true nếu ID mạng xã hội không bị null (đã liên kết)
                GoogleLinked = !string.IsNullOrEmpty(user.GoogleId),
                FacebookLinked = !string.IsNullOrEmpty(user.FacebookId),
            };

            return Ok(
                new
                {
                    profile = response,
                    socialLinks = new
                    {
                        googleLinked = response.GoogleLinked,
                        facebookLinked = response.FacebookLinked,
                        email = response.Email,
                    },
                }
            );
        }

        // 2. CẬP NHẬT THÔNG TIN HỒ SƠ (PUT /user/profile)
        [HttpPut("profile")]
        public IActionResult UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            int userId = GetCurrentUserId();
            var user = _context.AppUsers.FirstOrDefault(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            // Cập nhật từng phần: Chỉ cập nhật khi dữ liệu được gửi lên (khác null)
            // Trường hợp người dùng cố tình gửi chuỗi rỗng ("") -> Có nghĩa là họ muốn xóa thông tin đó đi.

            if (request.FullName != null)
                user.FullName = request.FullName;

            if (request.PhoneNumber != null)
                user.PhoneNumber = request.PhoneNumber;


            if (request.Province != null)
                user.Province = request.Province;

            if (request.Ward != null)
                user.Ward = request.Ward;

            if (request.Street != null)
                user.Street = request.Street;

            _context.SaveChanges();

            return Ok(new { message = "Cập nhật hồ sơ thành công!" });
        }

        // 3. ĐỔI MẬT KHẨU (PUT /user/change-password)
        [HttpPut("change-password")]
        public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
        {
            int userId = GetCurrentUserId();
            var user = _context.AppUsers.FirstOrDefault(u => u.Id == userId);

            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng" });

            // Nếu người dùng đăng nhập bằng Google/FB và chưa từng có mật khẩu,
            // họ không thể "đổi" mật khẩu (bạn có thể phát triển tính năng "Tạo mật khẩu" riêng cho case này)
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                return BadRequest(
                    new
                    {
                        message = "Tài khoản của bạn được liên kết qua mạng xã hội, không sử dụng mật khẩu.",
                    }
                );
            }

            // Kiểm tra mật khẩu cũ có đúng không
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Mật khẩu hiện tại không chính xác!" });
            }

            // Băm và lưu mật khẩu mới
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _context.SaveChanges();

            return Ok(new { message = "Đổi mật khẩu thành công!" });
        }
    }
}
