using farm2homeWebApi;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

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
            _context.Users.Add(newUser);
            _context.SaveChanges(); // Lưu vĩnh viễn vào DB thật
            return Ok(newUser);
        }

        // --- UPDATE ---
        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, [FromBody] User updatedUser)
        {
            var user = _context.Users.FirstOrDefault(u => u.id == id);
            if (user == null) return NotFound();

            user.name = updatedUser.name;
            _context.SaveChanges(); // Cập nhật vào DB
            return Ok(user);
        }

        // --- DELETE ---
        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.id == id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
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