
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
            new User { id = 1, name = "Nguyen Van A" },
            new User { id = 2, name = "Tran Thi B" },
            new User { id = 3, name = "Le Van C" }
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
    }

    // Model map với table users
    public class User
    {
        public int id { get; set; }
        public string name { get; set; }
    }
}