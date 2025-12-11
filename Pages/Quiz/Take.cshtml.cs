using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LMSPlatform.Models;
using LMSPlatform.Services;

namespace LMSPlatform.Pages.Quiz;

[Authorize]
public class TakeModel : PageModel
{
    private readonly IQuizService _quizService;
    private readonly IUserService _userService;
    private readonly ILogger<TakeModel> _logger;

    public TakeModel(
        IQuizService quizService,
        IUserService userService,
        ILogger<TakeModel> logger)
    {
        _quizService = quizService;
        _userService = userService;
        _logger = logger;
    }

    public Models.Quiz? Quiz { get; set; }
    public int AttemptId { get; set; }
    public int RemainingAttempts { get; set; }
    public QuizAttempt? PreviousAttempt { get; set; }

    public async Task<IActionResult> OnGetAsync(int quizId)
    {
        // Get current user
        var user = await _userService.GetUserByEmailAsync(User.Identity?.Name ?? "");
        if (user == null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        // Get quiz details
        Quiz = await _quizService.GetQuizByIdAsync(quizId);
        if (Quiz == null)
        {
            return NotFound("Quiz not found");
        }

        // Check if user can take the quiz
        if (!await _quizService.CanUserTakeQuizAsync(user.Id, quizId))
        {
            TempData["ErrorMessage"] = "You cannot take this quiz. You may have reached the maximum number of attempts or don't have access to the lesson.";
            
            if (Quiz.LessonID.HasValue)
            {
                return RedirectToPage("/Lessons/Watch", new { lessonId = Quiz.LessonID.Value });
            }
            else
            {
                return RedirectToPage("/Dashboard/Index");
            }
        }

        // Get remaining attempts
        RemainingAttempts = await _quizService.GetRemainingAttemptsAsync(user.Id, quizId);

        // Get previous attempt for reference
        PreviousAttempt = await _quizService.GetLatestAttemptAsync(user.Id, quizId);

        // Start new quiz attempt
        try
        {
            var attempt = await _quizService.StartQuizAttemptAsync(user.Id, quizId);
            AttemptId = attempt.AttemptID;

            _logger.LogInformation("Quiz attempt started: {AttemptId} for User {UserId}, Quiz {QuizId}", 
                AttemptId, user.Id, quizId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting quiz attempt for User {UserId}, Quiz {QuizId}", user.Id, quizId);
            TempData["ErrorMessage"] = "Unable to start quiz attempt: " + ex.Message;
            return RedirectToPage("/Dashboard/Index");
        }

        return Page();
    }
}