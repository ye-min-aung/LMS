using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LMSPlatform.Services;

namespace LMSPlatform.Controllers;

[ApiController]
[Route("api/progress")]
[Authorize]
public class ProgressApiController : ControllerBase
{
    private readonly ILessonProgressService _progressService;
    private readonly IUserService _userService;
    private readonly ILogger<ProgressApiController> _logger;

    public ProgressApiController(
        ILessonProgressService progressService,
        IUserService userService,
        ILogger<ProgressApiController> logger)
    {
        _progressService = progressService;
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressRequest request)
    {
        try
        {
            var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
            if (user == null)
            {
                return Unauthorized();
            }

            await _progressService.SaveVideoTimestampAsync(
                request.EnrollmentId, 
                request.LessonId, 
                request.Timestamp);

            _logger.LogInformation("Progress updated for user {UserId}, lesson {LessonId}, timestamp {Timestamp}", 
                user.Id, request.LessonId, request.Timestamp);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating progress for lesson {LessonId}", request.LessonId);
            return BadRequest(new { error = "Failed to update progress" });
        }
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompleteLesson([FromBody] CompleteLessonRequest request)
    {
        try
        {
            var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
            if (user == null)
            {
                return Unauthorized();
            }

            await _progressService.UpdateLessonProgressAsync(
                request.EnrollmentId, 
                request.LessonId, 
                completed: true);

            _logger.LogInformation("Lesson completed for user {UserId}, lesson {LessonId}", 
                user.Id, request.LessonId);

            return Ok(new { success = true, message = "Lesson marked as complete" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing lesson {LessonId}", request.LessonId);
            return BadRequest(new { error = "Failed to complete lesson" });
        }
    }

    [HttpGet("resume/{enrollmentId:int}/{lessonId:int}")]
    public async Task<IActionResult> GetResumeInfo(int enrollmentId, int lessonId)
    {
        try
        {
            var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
            if (user == null)
            {
                return Unauthorized();
            }

            var resumeInfo = await _progressService.GetLessonResumeInfoAsync(enrollmentId, lessonId);
            if (resumeInfo == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                canResume = resumeInfo.CanResume,
                timestamp = resumeInfo.VideoTimestamp,
                isCompleted = resumeInfo.IsCompleted,
                lastAccessedAt = resumeInfo.LastAccessedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resume info for lesson {LessonId}", lessonId);
            return BadRequest(new { error = "Failed to get resume information" });
        }
    }
}

public class UpdateProgressRequest
{
    public int EnrollmentId { get; set; }
    public int LessonId { get; set; }
    public int Timestamp { get; set; }
}

public class CompleteLessonRequest
{
    public int EnrollmentId { get; set; }
    public int LessonId { get; set; }
}