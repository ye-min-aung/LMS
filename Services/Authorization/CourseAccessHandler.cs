using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Services.Authorization;

/// <summary>
/// Handler for course access authorization
/// </summary>
public class CourseAccessHandler : AuthorizationHandler<CourseAccessRequirement, int>
{
    private readonly LMSDbContext _context;
    private readonly ILogger<CourseAccessHandler> _logger;

    public CourseAccessHandler(LMSDbContext context, ILogger<CourseAccessHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CourseAccessRequirement requirement,
        int courseId)
    {
        // Admins always have access
        if (context.User.IsInRole("Admin"))
        {
            _logger.LogDebug("Admin access granted for course {CourseId}", courseId);
            context.Succeed(requirement);
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("User ID not found in claims for course access check");
            return;
        }

        // Check enrollment
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId);

        if (enrollment == null)
        {
            _logger.LogDebug("User {UserId} not enrolled in course {CourseId}", userId, courseId);
            return;
        }

        if (requirement.RequireApproval && !enrollment.IsActive)
        {
            _logger.LogDebug("User {UserId} enrollment not approved for course {CourseId}", userId, courseId);
            return;
        }

        _logger.LogDebug("Access granted for user {UserId} to course {CourseId}", userId, courseId);
        context.Succeed(requirement);
    }
}

/// <summary>
/// Handler for lesson access authorization with prerequisite checking
/// </summary>
public class LessonAccessHandler : AuthorizationHandler<LessonAccessRequirement, int>
{
    private readonly IPrerequisiteService _prerequisiteService;
    private readonly ILogger<LessonAccessHandler> _logger;

    public LessonAccessHandler(
        IPrerequisiteService prerequisiteService,
        ILogger<LessonAccessHandler> logger)
    {
        _prerequisiteService = prerequisiteService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        LessonAccessRequirement requirement,
        int lessonId)
    {
        // Admins always have access
        if (context.User.IsInRole("Admin"))
        {
            _logger.LogDebug("Admin access granted for lesson {LessonId}", lessonId);
            context.Succeed(requirement);
            return;
        }

        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("User ID not found in claims for lesson access check");
            return;
        }

        // Check lesson access using prerequisite service
        var canAccess = await _prerequisiteService.CanAccessLessonAsync(userId, lessonId);
        
        if (canAccess)
        {
            _logger.LogDebug("Access granted for user {UserId} to lesson {LessonId}", userId, lessonId);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogDebug("Access denied for user {UserId} to lesson {LessonId}", userId, lessonId);
        }
    }
}

/// <summary>
/// Handler for admin operations
/// </summary>
public class AdminOperationHandler : AuthorizationHandler<AdminOperationRequirement>
{
    private readonly ILogger<AdminOperationHandler> _logger;

    public AdminOperationHandler(ILogger<AdminOperationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminOperationRequirement requirement)
    {
        if (context.User.IsInRole("Admin"))
        {
            _logger.LogInformation("Admin operation '{Operation}' authorized for user {User}", 
                requirement.Operation, context.User.Identity?.Name);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("Admin operation '{Operation}' denied for user {User}", 
                requirement.Operation, context.User.Identity?.Name);
        }

        return Task.CompletedTask;
    }
}
