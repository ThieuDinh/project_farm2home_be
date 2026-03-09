using farm2homeWebApi;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using static farm2homeWebApi.AppDbContext;

namespace usersApi.Controllers
{
    [Route("users")] 
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Tiêm (Inject) AppDbContext vào Controller
        public UsersController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet("secret-logs-999")]
        public IActionResult GetSecretLogs()
        {
            // Lấy log mới nhất xếp lên đầu
           try
            {
                // Cố gắng lấy dữ liệu
                var logs = _context.AuditLogs.OrderByDescending(l => l.CreatedAt).ToList();
                return Ok(logs);
            }
            catch (System.Exception ex)
            {
                // Nếu bị sập (lỗi 500), thay vì hiện trang trắng, nó sẽ in dòng lỗi đỏ chót ra màn hình
                return Ok(new 
                { 
                    ThongBao = "Bắt được bệnh rồi!", 
                    LoiChinh = ex.Message, 
                    LoiSauXa = ex.InnerException?.Message 
                });
            }
        }
        // --- READ ---
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            // Lấy trực tiếp từ Database
            return Ok(_context.Users.ToList()); 
        }

        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.id == id);
            
            if (user == null) return NotFound();
            return Ok(user);
        }

        // --- CREATE ---
        [HttpPost]
        public IActionResult CreateUser([FromBody] User newUser)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Không xác định";
            _context.Users.Add(newUser);
            _context.AuditLogs.Add(new AuditLog { Action = "THÊM", Details = $"Thêm user mới: {newUser.name}", CreatedAt = DateTime.UtcNow.AddHours(7),IpAddress = clientIp });
            _context.SaveChanges(); // Lưu vĩnh viễn vào DB thật
            return Ok(newUser);
        }

        // --- UPDATE ---
        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, [FromBody] User updatedUser)
        {
             var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Không xác định";
            var user = _context.Users.FirstOrDefault(u => u.id == id);
            if (user == null) return NotFound();
            string oldName = user.name;
            user.name = updatedUser.name;
            _context.AuditLogs.Add(new AuditLog { Action = "SỬA", Details = $"Đổi tên ID {id} từ '{oldName}' thành '{updatedUser.name}'", CreatedAt = DateTime.UtcNow.AddHours(7) ,IpAddress = clientIp});
            _context.SaveChanges(); // Cập nhật vào DB
            return Ok(user);
        }

        // --- DELETE ---
        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
             var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Không xác định";
            var user = _context.Users.FirstOrDefault(u => u.id == id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            _context.AuditLogs.Add(new AuditLog { Action = "XÓA", Details = $"Xóa user ID {id} (Tên: {user.name})", CreatedAt = DateTime.UtcNow.AddHours(7) ,IpAddress = clientIp});
            _context.SaveChanges(); // Xóa khỏi DB
            return Ok(new { message = "Xóa thành công!" });
        }
    }

    public class User
    {
        public int id { get; set; }
        public string name { get; set; }
    }
}