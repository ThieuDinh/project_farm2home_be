using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using farm2homeWebApi.Data;
using farm2homeWebApi.DTOs;
using farm2homeWebApi.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace farm2homeWebApi.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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

                IsEmailVerified = false,
            };

            // 4. Lưu vào Database
            _context.AppUsers.Add(newUser);
            _context.SaveChanges();

            return Ok(
                new
                {
                    message = "Đăng ký thành công! Vui lòng kiểm tra email để xác thực.",
                    userEmail = newUser.Email,
                }
            );
        }

        //login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _context.AppUsers.FirstOrDefault(u => u.Email == request.Email);

            // Kiểm tra user tồn tại và mật khẩu khớp (dùng BCrypt để so khớp hash)
            if (
                user == null
                || string.IsNullOrEmpty(user.PasswordHash)
                || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)
            )
            {
                return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác" });
            }

            var token = GenerateJwtToken(user);
            return Ok(new { token, user = new { user.Email, user.FullName } });
        }

        // --- ĐĂNG NHẬP GOOGLE / FACEBOOK ---
        [HttpPost("external-login")]
        public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest request)
        {
            string providerId = null;
            string email = null;
            string name = null;

            if (request.Provider == "Google")
            {
                try
                {
                    var settings = new GoogleJsonWebSignature.ValidationSettings()
                    {
                        Audience = new List<string>()
                        {
                            _configuration["Authentication:Google:ClientId"],
                        },
                    };
                    var payload = await GoogleJsonWebSignature.ValidateAsync(
                        request.Token,
                        settings
                    );
                    providerId = payload.Subject;
                    email = payload.Email;
                    name = payload.Name;
                }
                catch (InvalidJwtException)
                {
                    return Unauthorized(new { message = "Token Google không hợp lệ." });
                }
            }
            else if (request.Provider == "Facebook")
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(
                        $"https://graph.facebook.com/me?fields=id,name,email&access_token={request.Token}"
                    );
                    if (!response.IsSuccessStatusCode)
                    {
                        return Unauthorized(new { message = "Token Facebook không hợp lệ." });
                    }
                    var stringResponse = await response.Content.ReadAsStringAsync();
                    var fbUser = System.Text.Json.JsonSerializer.Deserialize<FacebookUserInfo>(
                        stringResponse
                    );
                    providerId = fbUser.id;
                    email = fbUser.email;
                    name = fbUser.name;

                    if (string.IsNullOrEmpty(email))
                    {
                        return BadRequest(
                            new { message = "Không thể lấy email từ tài khoản Facebook này." }
                        );
                    }
                }
                catch
                {
                    return Unauthorized(new { message = "Xác thực Facebook thất bại." });
                }
            }
            else
            {
                return BadRequest(new { message = "Provider không được hỗ trợ." });
            }

            // Bước 1: Tìm xem ID mạng xã hội này đã liên kết với tài khoản nào chưa
            AppUser user = null;
            if (request.Provider == "Google")
                user = _context.AppUsers.FirstOrDefault(u => u.GoogleId == providerId);
            else if (request.Provider == "Facebook")
                user = _context.AppUsers.FirstOrDefault(u => u.FacebookId == providerId);

            if (user != null)
            {
                // Đã từng liên kết -> Cho đăng nhập luôn
                var token = GenerateJwtToken(user);
                return Ok(
                    new
                    {
                        token,
                        status = "Success",
                        message = "Đăng nhập thành công",
                        user = new { user.Email, user.FullName },
                    }
                );
            }

            // Bước 2: Nếu chưa có ID mạng xã hội, kiểm tra theo Email trong DB (nếu email != null)
            if (!string.IsNullOrEmpty(email))
            {
                user = _context.AppUsers.FirstOrDefault(u => u.Email == email);
            }

            if (user != null)
            {
                // TÌM THẤY EMAIL TRÙNG -> LIÊN KẾT TÀI KHOẢN CŨ
                if (request.Provider == "Google")
                    user.GoogleId = providerId;
                else if (request.Provider == "Facebook")
                    user.FacebookId = providerId;

                _context.SaveChanges();

                var token = GenerateJwtToken(user);
                return Ok(
                    new
                    {
                        token,
                        status = "Linked",
                        message = "Đã liên kết với tài khoản cũ thành công",
                        user = new { user.Email, user.FullName },
                    }
                );
            }
            else
            {
                // CHƯA CÓ EMAIL TRÙNG -> TẠO TÀI KHOẢN MỚI
                var newUser = new AppUser
                {
                    Email = email ?? $"{providerId}@{request.Provider.ToLower()}.com", // Phòng trường hợp thiếu email
                    FullName = name ?? "User",
                    IsEmailVerified = true, // Vì Google/FB đã xác thực email này
                    CreatedAt = DateTime.UtcNow,
                };

                if (request.Provider == "Google")
                    newUser.GoogleId = providerId;
                else if (request.Provider == "Facebook")
                    newUser.FacebookId = providerId;

                _context.AppUsers.Add(newUser);
                _context.SaveChanges();

                var token = GenerateJwtToken(newUser);
                return Ok(
                    new
                    {
                        token,
                        status = "Created",
                        message = "Đã tạo tài khoản mới thành công",
                        user = new { newUser.Email, newUser.FullName },
                    }
                );
            }
        }

        // Hàm hỗ trợ tạo Token JWT (Bạn sẽ cần cài thư viện System.IdentityModel.Tokens.Jwt)
        private string GenerateJwtToken(AppUser user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

            // 1. Tạo danh sách các thông tin (Claims) sẽ được nhúng vào Token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("FullName", user.FullName), // Custom claim
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // ID duy nhất của token
            };

            // 2. Thiết lập cấu trúc Token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7), // Cho phép đăng nhập trong 7 ngày
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                ),
            };

            // 3. Tiến hành đóng gói và xuất mã
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
