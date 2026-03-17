public class RegisterRequest
{
    public string Email { get; set; }
    public string Password { get; set; } // Nhận mật khẩu gốc từ FE để băm
    public string FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Province { get; set; } 
     
    public string? Ward { get; set; }     
    public string? Street { get; set; }
    public int? Age { get; set; }
}