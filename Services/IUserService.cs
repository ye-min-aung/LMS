using Microsoft.AspNetCore.Identity;
using LMSPlatform.Models;

namespace LMSPlatform.Services;

public interface IUserService
{
    Task<User> RegisterAsync(RegisterModel model);
    Task<SignInResult> LoginAsync(LoginModel model);
    Task<bool> ResetPasswordAsync(string email);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<bool> ApproveStudentAsync(int userId);
    Task<bool> ChangeUserRoleAsync(int userId, string newRole);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<bool> IsUserApprovedStudentAsync(int userId);
    Task<bool> UpdateUserProfileAsync(int userId, string fullName);
}

public class RegisterModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class LoginModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
}