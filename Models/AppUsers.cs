public class AppUser
{
    public int Id { get; set; }
    
    // Thông tin đăng nhập
    public string Email { get; set; }
    public string PasswordHash { get; set; } // Tuyệt đối không lưu mật khẩu gốc
    
    // Thông tin cá nhân
    public string FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Province { get; set; } // Tỉnh/Thành phố

public string? Ward { get; set; }     // Phường/Xã
public string? Street { get; set; }
    public int? Age { get; set; }
    
    // Trạng thái xác thực
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; } // Mã gửi qua email
    
    // Dùng cho đăng nhập Google/Facebook
    public string? GoogleId { get; set; } 
    public string? FacebookId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedAt { get;}
}