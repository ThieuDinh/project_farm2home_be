using System.Text;
using farm2homeWebApi.Data;
using farm2homeWebApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("\n[JWT DEBUG] LỖI XÁC THỰC: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("\n[JWT DEBUG] TOKEN HỢP LỆ!");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine(
                    "\n[JWT DEBUG] BỊ TỪ CHỐI (401): "
                        + context.Error
                        + " - "
                        + context.ErrorDescription
                );
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization(); // Thêm dịch vụ phân quyền

// 1. THÊM DÒNG NÀY: Báo cho chương trình biết chúng ta sẽ xài Controller
builder.Services.AddControllers();

// Bật CORS cho Vercel (React) gọi được API (Như đã hướng dẫn trước đó)
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    );
});

// Đăng ký kết nối SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var app = builder.Build();

// Hỗ trợ Forwarded Headers (để nhận diện HTTPS/IP chính xác khi chạy sau Proxy của Host)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Luôn hiển thị Swagger (kể cả trên Host) theo yêu cầu của bạn
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Farm2Home API v1");
    options.RoutePrefix = string.Empty; // Truy cập Swagger ngay tại trang chủ API
});

app.UseStaticFiles();
app.UseRouting(); // Kích hoạt bộ định tuyến trước khi dùng CORS

// Cấu hình CORS để cho phép FE gọi API
app.UseCors("AllowAll");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // db.Database.EnsureCreated(); // EnsureCreated chỉ tạo DB nếu chưa có, không chạy migration
    db.Database.Migrate(); // Tự động chạy tất cả migration còn thiếu khi startup
}
app.UseAuthentication();
app.UseAuthorization(); // Thêm Middleware phân quyền ở đây!

// 2. THÊM DÒNG NÀY: Kích hoạt bộ định tuyến để nó dò tìm [Route("users")] của bạn
app.MapControllers();

app.Run();
