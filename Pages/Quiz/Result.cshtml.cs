using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LMSPlatform.Models;
using LMSPlatform.Services;

namespace LMSPlatform.Pages.Quiz;

[Authorize]
public class ResultModel : PageModel
{
    private readonly IQuizService _quizService;
    private readonly IUserService _userService;
    private readonly ILogger<ResultModel> _logger;

    public ResultModel(
        IQuizService quizService,
        IUserService userService,
        ILogger<ResultModel> logger)
    {
        _quizService = quizService;
        _userService = userService;
        _logger = logger;
    }

    public Models.Quiz? Quiz { get; set; }
    public QuizResult? Result { get; set; }
    public int AttemptId { get; set; }
    public int RemainingAttempts { get; set; }
    public List<QuizAttempt> PreviousAttempts { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int attemptId)
    {
        // Get current user
        var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
        if (user == null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        AttemptId = attemptId;

        // Get quiz attempt
        var attempt = await _quizService.GetQuizAttemptAsync(attemptId);
        if (attempt == null)
        {
            return NotFound("Quiz attempt not found");
        }

        // Verify user owns this attempt
        if (attempt.UserID != user.Id)
        {
            return Forbid();
        }

        Quiz = attempt.Quiz;

        // Calculate results if not already completed
        if (!attempt.IsCompleted)
        {
            return RedirectToPage("/Quiz/Take", new { quizId = Quiz.QuizID });
        }

        try
        {
            Result = await _quizService.CalculateQuizScoreAsync(attemptId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating quiz results for attempt {AttemptId}", attemptId);
            TempData["ErrorMessage"] = "Unable to load quiz results.";
            return RedirectToPage("/Dashboard/Index");
        }

        // Get remaining attempts
        RemainingAttempts = await _quizService.GetRemainingAttemptsAsync(user.Id, Quiz.QuizID);

        // Get all previous attempts for history
        PreviousAttempts = (await _quizService.GetUserQuizAttemptsAsync(user.Id, Quiz.QuizID)).ToList();

        return Page();
    }
}