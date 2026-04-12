namespace farm2homeWebApi.DTOs;

public class AdminUserDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; }
    public bool IsBanned { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; }
    public string FullName { get; set; }
    public string Password { get; set; }
    public string Role { get; set; } // Admin | Customer
}

public class ResetPasswordRequest
{
    public string NewPassword { get; set; }
}

public class ToggleBanRequest
{
    public bool IsBanned { get; set; }
}

public class UpdateRoleRequest
{
    public string Role { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; }
}
