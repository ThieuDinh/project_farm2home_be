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
    db.Database.EnsureCreated(); 
    
    // Tạo bảng nếu chưa có
    db.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' and xtype='U') CREATE TABLE AuditLogs (Id INT IDENTITY(1,1) PRIMARY KEY, Action NVARCHAR(50), Details NVARCHAR(MAX), CreatedAt DATETIME2);");
    
    // Tự động thêm cột IpAddress nếu bảng cũ chưa có cột này
    db.Database.ExecuteSqlRaw("IF COL_LENGTH('AuditLogs', 'IpAddress') IS NULL ALTER TABLE AuditLogs ADD IpAddress NVARCHAR(50);");
}
// 2. THÊM DÒNG NÀY: Kích hoạt bộ định tuyến để nó dò tìm [Route("users")] của bạn
app.MapControllers();

app.Run();