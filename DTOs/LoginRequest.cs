namespace farm2homeWebApi.DTOs
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    // DTO cho đăng nhập mạng xã hội
    public class ExternalLoginRequest
    {
        public string Provider { get; set; } // "Google" hoặc "Facebook"
        public string Token { get; set; } // id_token của Google hoặc access_token của Facebook
    }

    // Helper model cho Facebook User
    public class FacebookUserInfo
    {
        public string id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
    }
}
