using farm2homeWebApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. THÊM DÒNG NÀY: Báo cho chương trình biết chúng ta sẽ xài Controller
builder.Services.AddControllers();

// Bật CORS cho Vercel (React) gọi được API (Như đã hướng dẫn trước đó)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});
// Đăng ký kết nối SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseCors("AllowAll");
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // Tự động tạo DB và Bảng
}
// 2. THÊM DÒNG NÀY: Kích hoạt bộ định tuyến để nó dò tìm [Route("users")] của bạn
app.MapControllers();

app.Run();