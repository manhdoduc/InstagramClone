using System.ComponentModel.DataAnnotations;

namespace InstagramClone.Web.Models.Auth;

/// <summary>Mirror of LoginUserDto from backend.</summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Mirror of RegisterUserDto from backend.</summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "Tên không được để trống")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ không được để trống")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username không được để trống")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Mirror of TokenResponseDto from backend.</summary>
public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Mirror of RegisteredUserDto from backend.</summary>
public class RegisteredUser
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Thông tin user được parse từ JWT Claims, lưu trong AuthState.
/// </summary>
public class CurrentUser
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsAuthenticated { get; set; }
}
