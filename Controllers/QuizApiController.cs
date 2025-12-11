using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LMSPlatform.Services;

namespace LMSPlatform.Controllers;

[ApiController]
[Route("api/quiz")]
[Authorize]
public class QuizApiController : ControllerBase
{
    private readonly IQuizService _quizService;
    private readonly IUserService _userService;
    private readonly ILogger<QuizApiController> _logger;

    public QuizApiController(
        IQuizService quizService,
        IUserService userService,
        ILogger<QuizApiController> logger)
    {
        _quizService = quizService;
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitQuiz([FromBody] SubmitQuizRequest request)
    {
        try
        {
            var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
            if (user == null)
            {
                return Unauthorized();
            }

            // Verify the attempt belongs to the current user
            var attempt = await _quizService.GetQuizAttemptAsync(request.AttemptId);
            if (attempt == null)
            {
                return BadRequest(new { error = "Quiz attempt not found" });
            }

            if (attempt.UserID != user.Id)
            {
                return Forbid();
            }

            if (attempt.IsCompleted)
            {
                return BadRequest(new { error = "Quiz attempt already completed" });
            }

            // Submit the quiz
            var submittedAttempt = await _quizService.SubmitQuizAttemptAsync(request.AttemptId, request.Answers);

            _logger.LogInformation("Quiz submitted successfully: AttemptId {AttemptId}, Score: {Score}%, Passed: {Passed}", 
                request.AttemptId, submittedAttempt.Score, submittedAttempt.Passed);

            return Ok(new 
            { 
                success = true, 
                attemptId = submittedAttempt.AttemptID,
                score = submittedAttempt.Score,
                passed = submittedAttempt.Passed,
                message = "Quiz submitted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting quiz attempt {AttemptId}", request.AttemptId);
            return BadRequest(new { error = "Failed to submit quiz: " + ex.Message });
        }
    }

    [HttpGet("attempt/{attemptId:int}/status")]
    public async Task<IActionResult> GetAttemptStatus(int attemptId)
    {
        try
        {
            var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
            if (user == null)
            {
                return Unauthorized();
            }

            var attempt = await _quizService.GetQuizAttemptAsync(attemptId);
            if (attempt == null)
            {
                return NotFound();
            }

            if (attempt.UserID != user.Id)
            {
                return Forbid();
            }

            return Ok(new
            {
                attemptId = attempt.AttemptID,
                isCompleted = attempt.IsCompleted,
                score = attempt.Score,
                passed = attempt.Passed,
                startedAt = attempt.StartedAt,
                completedAt = attempt.CompletedAt,
                duration = attempt.GetDuration()?.TotalMinutes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attempt status for {AttemptId}", attemptId);
            return BadRequest(new { error = "Failed to get attempt status" });
        }
    }

    [HttpGet("{quizId:int}/attempts")]
    public async Task<IActionResult> GetUserAttempts(int quizId)
    {
        try
        {
            var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
            if (user == null)
            {
                return Unauthorized();
            }

            var attempts = await _quizService.GetUserQuizAttemptsAsync(user.Id, quizId);
            var remainingAttempts = await _quizService.GetRemainingAttemptsAsync(user.Id, quizId);

            return Ok(new
            {
                attempts = attempts.Select(a => new
                {
                    attemptId = a.AttemptID,
                    attemptNumber = a.AttemptNumber,
                    score = a.Score,
                    passed = a.Passed,
                    startedAt = a.StartedAt,
                    completedAt = a.CompletedAt,
                    isCompleted = a.IsCompleted
                }),
                remainingAttempts = remainingAttempts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user attempts for quiz {QuizId}", quizId);
            return BadRequest(new { error = "Failed to get attempts" });
        }
    }
}

public class SubmitQuizRequest
{
    public int AttemptId { get; set; }
    public List<LMSPlatform.Services.QuizAnswerModel> Answers { get; set; } = new();
}