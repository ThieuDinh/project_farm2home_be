
using Microsoft.AspNetCore.Mvc;

namespace usersApi.Controllers
{
    [Route("users")] 
    [ApiController]
    public class UsersController : ControllerBase
    {
        // Tạo dữ liệu ảo cho bảng users(id, name)
        private static List<User> _users = new List<User>
        {
            new User { id = 1, name = "Nguyễn Hoàng Lợi" },
            new User { id = 2, name = "Lê Hữu Luân" },
            new User { id = 3, name = "Nguyễn Phúc Sang" },
            new User { id = 4, name = "Phan Ngọc Thao" },
            new User { id = 5, name = "Võ Đình Thiệu" },
            new User { id = 6, name = "Bùi Nguyễn Minh Vy" }
        };

        // 1. API lấy tất cả user -> BASE_API/users
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            return Ok(_users);
        }

        // 2. API lấy user theo id -> BASE_API/users/1
        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = _users.FirstOrDefault(u => u.id == id);
            if (user == null) 
            {
                return NotFound(); // Trả về lỗi 404 nếu không tìm thấy
            }
            return Ok(user);
        }
        [HttpPost]
        public IActionResult CreateUser([FromBody] User newUser)
        {
            // Tự động tăng ID
            newUser.id = _users.Any() ? _users.Max(u => u.id) + 1 : 1;
            _users.Add(newUser);
            return Ok(newUser); // Trả về user vừa tạo thành công
        }

        // --- UPDATE (Sửa) ---
        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, [FromBody] User updatedUser)
        {
            var user = _users.FirstOrDefault(u => u.id == id);
            if (user == null) return NotFound();

            user.name = updatedUser.name; // Cập nhật tên
            return Ok(user);
        }

        // --- DELETE (Xóa) ---
        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _users.FirstOrDefault(u => u.id == id);
            if (user == null) return NotFound();

            _users.Remove(user);
            return Ok(new { message = "Xóa thành công!" });
        }
    }

    // Model map với table users
    public class User
    {
        public int id { get; set; }
        public string name { get; set; }
    }
}