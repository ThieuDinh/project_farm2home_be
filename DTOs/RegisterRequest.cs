using System.ComponentModel.DataAnnotations;

namespace farm2homeWebApi.DTOs{
public class RegisterRequest
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    public string PhoneNumber { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")] // Khớp với placeholder trên FE
    public string Password { get; set; }

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu đã nhập")]
    public string ConfirmPassword { get; set; }
}}