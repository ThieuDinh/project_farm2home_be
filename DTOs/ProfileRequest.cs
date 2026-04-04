namespace farm2homeWebApi.DTOs
{
    // DTO trả dữ liệu về cho ReactJS
    public class UserProfileResponse
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public string? PhoneNumber { get; set; }
       
        public string? Province { get; set; }
        public string? Ward { get; set; }
        public string? Street { get; set; }
        public bool GoogleLinked { get; set; }
        public bool FacebookLinked { get; set; }
    }

    // DTO nhận dữ liệu cập nhật hồ sơ từ ReactJS
    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }

        public string? PhoneNumber { get; set; }

     
        public string? Province { get; set; }
        public string? Ward { get; set; }
        public string? Street { get; set; }
    }

    // DTO nhận dữ liệu đổi mật khẩu
    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
