using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Services;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly LMSDbContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public UserService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        LMSDbContext context,
        ILogger<UserService> logger,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<User> RegisterAsync(RegisterModel model)
    {
        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Role = UserRoles.Student,
            IsApprovedStudent = false
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User registration failed: {errors}");
        }

        // Add user to Student role
        await _userManager.AddToRoleAsync(user, UserRoles.Student);

        _logger.LogInformation("New user registered: {Email}", model.Email);
        return user;
    }

    public async Task<SignInResult> LoginAsync(LoginModel model)
    {
        var result = await _signInManager.PasswordSignInAsync(
            model.Email, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in: {Email}", model.Email);
        }
        else if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out: {Email}", model.Email);
        }
        else
        {
            _logger.LogWarning("Failed login attempt for: {Email}", model.Email);
        }

        return result;
    }

    public async Task<bool> ResetPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return true;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        // Build the reset link
        var baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:5001";
        var encodedToken = System.Web.HttpUtility.UrlEncode(token);
        var resetLink = $"{baseUrl}/Identity/Account/ResetPassword?code={encodedToken}";
        
        // Send email with reset link
        await _emailService.SendPasswordResetEmailAsync(email, resetLink);
        
        _logger.LogInformation("Password reset email sent to {Email}", email);

        return true;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.Enrollments)
                .ThenInclude(e => e.Course)
            .Include(u => u.Certificates)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<bool> ApproveStudentAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.Role != UserRoles.Student)
        {
            return false;
        }

        user.IsApprovedStudent = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Student approved: {UserId}", userId);
        return true;
    }

    public async Task<bool> ChangeUserRoleAsync(int userId, string newRole)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        // Remove from current roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // Add to new role
        await _userManager.AddToRoleAsync(user, newRole);

        // Update user entity
        user.Role = newRole;
        if (newRole == UserRoles.Admin)
        {
            user.IsApprovedStudent = true;
        }

        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User role changed: {UserId} to {Role}", userId, newRole);
        return true;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _context.Users
            .Include(u => u.Enrollments)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<bool> IsUserApprovedStudentAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.IsApprovedStudent == true;
    }

    public async Task<bool> UpdateUserProfileAsync(int userId, string fullName)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.FullName = fullName;
        await _context.SaveChangesAsync();

        return true;
    }
}