using Microsoft.EntityFrameworkCore;
using usersApi.Controllers; // Để nó nhận diện được class User

namespace farm2homeWebApi
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Đại diện cho bảng Users trong Database
        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; }   // Hành động (THÊM, SỬA, XÓA)
        public string Details { get; set; }  // Chi tiết (Xóa ai, thêm ai)
        public DateTime CreatedAt { get; set; } // Thời gian
        public string IpAddress { get; set; }
    }
    }

    
}