using Microsoft.EntityFrameworkCore;
using farm2homeWebApi.Models;
using usersApi.Controllers; // Giữ lại tạm thời cho class User nếu chưa dời đi

namespace farm2homeWebApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Đại diện cho bảng Users trong Database
        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public class AuditLog
        {
            public int Id { get; set; }
            public string Action { get; set; }   // Hành động (THÊM, SỬA, XÓA)
            public string Details { get; set; }  // Chi tiết (Xóa ai, thêm ai)
            public DateTime CreatedAt { get; set; } // Thời gian
            public string? IpAddress { get; set; }
        }
    }


}