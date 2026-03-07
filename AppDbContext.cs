using Microsoft.EntityFrameworkCore;
using usersApi.Controllers; // Để nó nhận diện được class User

namespace farm2homeWebApi
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Đại diện cho bảng Users trong Database
        public DbSet<User> Users { get; set; }
    }

    
}