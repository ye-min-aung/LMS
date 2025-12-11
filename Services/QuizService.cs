using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Services;

public class QuizService : IQuizService
{
    private readonly LMSDbContext _context;
    private readonly IPrerequisiteService _prerequisiteService;
    private readonly ILogger<QuizService> _logger;

    public QuizService(
        LMSDbContext context,
        IPrerequisiteService prerequisiteService,
        ILogger<QuizService> logger)
    {
        _context = context;
        _prerequisiteService = prerequisiteService;
        _logger = logger;
    }

    #region Quiz Management

    public async Task<Quiz?> GetQuizByIdAsync(int quizId)
    {
        return await _context.Quizzes
            .Include(q => q.Questions.OrderBy(q => q.QuestionOrder))
                .ThenInclude(q => q.AnswerChoices.OrderBy(ac => ac.ChoiceOrder))
            .Include(q => q.Lesson)
                .ThenInclude(l => l!.Module)
                    .ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(q => q.QuizID == quizId);
    }

    public async Task<Quiz> CreateQuizAsync(CreateQuizModel model)
    {
        var quiz = new Quiz
        {
            LessonID = model.LessonID,
            Title = model.Title,
            Instructions = model.Instructions,
            RequiredToUnlock = model.RequiredToUnlock,
            PassingScore = model.PassingScore,
            MaxAttempts = model.MaxAttempts
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Quiz created: {QuizId} - {Title}", quiz.QuizID, quiz.Title);
        return quiz;
    }

    public async Task<bool> UpdateQuizAsync(int quizId, UpdateQuizModel model)
    {
        var quiz = await _context.Quizzes.FindAsync(quizId);
        if (quiz == null)
        {
            return false;
        }

        quiz.Title = model.Title;
        quiz.Instructions = model.Instructions;
        quiz.RequiredToUnlock = model.RequiredToUnlock;
        quiz.PassingScore = model.PassingScore;
        quiz.MaxAttempts = model.MaxAttempts;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Quiz updated: {QuizId}", quizId);
        return true;
    }

    public async Task<bool> DeleteQuizAsync(int quizId)
    {
        var quiz = await _context.Quizzes.FindAsync(quizId);
        if (quiz == null)
        {
            return false;
        }

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Quiz deleted: {QuizId}", quizId);
        return true;
    }

    public async Task<IEnumerable<Quiz>> GetLessonQuizzesAsync(int lessonId)
    {
        return await _context.Quizzes
            .Where(q => q.LessonID == lessonId)
            .Include(q => q.Questions)
            .OrderBy(q => q.CreatedAt)
            .ToListAsync();
    }

    #endregion

    #region Question Management

    public async Task<Question> CreateQuestionAsync(CreateQuestionModel model)
    {
        var question = new Question
        {
            QuizID = model.QuizID,
            QuestionText = model.QuestionText,
            QuestionOrder = model.QuestionOrder,
            Points = model.Points
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Question created: {QuestionId} for Quiz {QuizId}", question.QuestionID, model.QuizID);
        return question;
    }

    public async Task<bool> UpdateQuestionAsync(int questionId, UpdateQuestionModel model)
    {
        var question = await _context.Questions.FindAsync(questionId);
        if (question == null)
        {
            return false;
        }

        question.QuestionText = model.QuestionText;
        question.QuestionOrder = model.QuestionOrder;
        question.Points = model.Points;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Question updated: {QuestionId}", questionId);
        return true;
    }

    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        var question = await _context.Questions.FindAsync(questionId);
        if (question == null)
        {
            return false;
        }

        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Question deleted: {QuestionId}", questionId);
        return true;
    }

    public async Task<IEnumerable<Question>> GetQuizQuestionsAsync(int quizId)
    {
        return await _context.Questions
            .Where(q => q.QuizID == quizId)
            .Include(q => q.AnswerChoices.OrderBy(ac => ac.ChoiceOrder))
            .OrderBy(q => q.QuestionOrder)
            .ToListAsync();
    }

    #endregion

    #region Answer Choice Management

    public async Task<AnswerChoice> CreateAnswerChoiceAsync(CreateAnswerChoiceModel model)
    {
        var choice = new AnswerChoice
        {
            QuestionID = model.QuestionID,
            ChoiceText = model.ChoiceText,
            IsCorrect = model.IsCorrect,
            ChoiceOrder = model.ChoiceOrder
        };

        _context.AnswerChoices.Add(choice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Answer choice created: {ChoiceId} for Question {QuestionId}", choice.ChoiceID, model.QuestionID);
        return choice;
    }

    public async Task<bool> UpdateAnswerChoiceAsync(int choiceId, UpdateAnswerChoiceModel model)
    {
        var choice = await _context.AnswerChoices.FindAsync(choiceId);
        if (choice == null)
        {
            return false;
        }

        choice.ChoiceText = model.ChoiceText;
        choice.IsCorrect = model.IsCorrect;
        choice.ChoiceOrder = model.ChoiceOrder;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Answer choice updated: {ChoiceId}", choiceId);
        return true;
    }

    public async Task<bool> DeleteAnswerChoiceAsync(int choiceId)
    {
        var choice = await _context.AnswerChoices.FindAsync(choiceId);
        if (choice == null)
        {
            return false;
        }

        _context.AnswerChoices.Remove(choice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Answer choice deleted: {ChoiceId}", choiceId);
        return true;
    }

    #endregion

    #region Quiz Taking

    public async Task<QuizAttempt> StartQuizAttemptAsync(int userId, int quizId)
    {
        // Check if user can take the quiz
        if (!await CanUserTakeQuizAsync(userId, quizId))
        {
            throw new InvalidOperationException("User cannot take this quiz");
        }

        // Get attempt number
        var previousAttempts = await _context.QuizAttempts
            .CountAsync(qa => qa.UserID == userId && qa.QuizID == quizId);

        var attempt = new QuizAttempt
        {
            UserID = userId,
            QuizID = quizId,
            AttemptNumber = previousAttempts + 1,
            StartedAt = DateTime.UtcNow
        };

        _context.QuizAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Quiz attempt started: {AttemptId} for User {UserId}, Quiz {QuizId}", attempt.AttemptID, userId, quizId);
        return attempt;
    }

    public async Task<QuizAttempt> SubmitQuizAttemptAsync(int attemptId, List<QuizAnswerModel> answers)
    {
        var attempt = await _context.QuizAttempts
            .Include(qa => qa.Quiz)
                .ThenInclude(q => q.Questions)
                    .ThenInclude(q => q.AnswerChoices)
            .FirstOrDefaultAsync(qa => qa.AttemptID == attemptId);

        if (attempt == null)
        {
            throw new ArgumentException("Quiz attempt not found");
        }

        if (attempt.IsCompleted)
        {
            throw new InvalidOperationException("Quiz attempt already completed");
        }

        // Store user answers
        attempt.UserAnswers = JsonSerializer.Serialize(answers);

        // Calculate score
        var result = await CalculateQuizScoreAsync(attemptId);
        attempt.CompleteAttempt(result.Score, result.Passed);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Quiz attempt submitted: {AttemptId}, Score: {Score}%, Passed: {Passed}", 
            attemptId, result.Score, result.Passed);

        return attempt;
    }

    public async Task<QuizAttempt?> GetQuizAttemptAsync(int attemptId)
    {
        return await _context.QuizAttempts
            .Include(qa => qa.Quiz)
                .ThenInclude(q => q.Questions)
                    .ThenInclude(q => q.AnswerChoices)
            .Include(qa => qa.User)
            .FirstOrDefaultAsync(qa => qa.AttemptID == attemptId);
    }

    public async Task<IEnumerable<QuizAttempt>> GetUserQuizAttemptsAsync(int userId, int quizId)
    {
        return await _context.QuizAttempts
            .Where(qa => qa.UserID == userId && qa.QuizID == quizId)
            .Include(qa => qa.Quiz)
            .OrderByDescending(qa => qa.StartedAt)
            .ToListAsync();
    }

    public async Task<QuizAttempt?> GetLatestAttemptAsync(int userId, int quizId)
    {
        return await _context.QuizAttempts
            .Where(qa => qa.UserID == userId && qa.QuizID == quizId)
            .Include(qa => qa.Quiz)
            .OrderByDescending(qa => qa.StartedAt)
            .FirstOrDefaultAsync();
    }

    #endregion

    #region Quiz Validation and Scoring

    public async Task<bool> CanUserTakeQuizAsync(int userId, int quizId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Lesson)
            .FirstOrDefaultAsync(q => q.QuizID == quizId);

        if (quiz == null)
        {
            return false;
        }

        // Check lesson access if quiz is attached to a lesson
        if (quiz.LessonID.HasValue)
        {
            if (!await _prerequisiteService.CanAccessLessonAsync(userId, quiz.LessonID.Value))
            {
                return false;
            }
        }

        // Check remaining attempts
        var remainingAttempts = await GetRemainingAttemptsAsync(userId, quizId);
        return remainingAttempts > 0;
    }

    public async Task<QuizResult> CalculateQuizScoreAsync(int attemptId)
    {
        var attempt = await GetQuizAttemptAsync(attemptId);
        if (attempt == null)
        {
            throw new ArgumentException("Quiz attempt not found");
        }

        var userAnswers = new List<QuizAnswerModel>();
        if (!string.IsNullOrEmpty(attempt.UserAnswers))
        {
            userAnswers = JsonSerializer.Deserialize<List<QuizAnswerModel>>(attempt.UserAnswers) ?? new();
        }

        var questions = attempt.Quiz.Questions.ToList();
        var totalPoints = questions.Sum(q => q.Points);
        var earnedPoints = 0;
        var correctAnswers = 0;
        var questionResults = new List<QuestionResult>();

        foreach (var question in questions)
        {
            var userAnswer = userAnswers.FirstOrDefault(ua => ua.QuestionID == question.QuestionID);
            var correctChoice = question.AnswerChoices.FirstOrDefault(ac => ac.IsCorrect);
            var selectedChoice = userAnswer != null 
                ? question.AnswerChoices.FirstOrDefault(ac => ac.ChoiceID == userAnswer.SelectedChoiceID)
                : null;

            var isCorrect = correctChoice != null && userAnswer != null && 
                           userAnswer.SelectedChoiceID == correctChoice.ChoiceID;

            if (isCorrect)
            {
                earnedPoints += question.Points;
                correctAnswers++;
            }

            questionResults.Add(new QuestionResult
            {
                QuestionID = question.QuestionID,
                QuestionText = question.QuestionText,
                SelectedChoiceID = userAnswer?.SelectedChoiceID ?? 0,
                CorrectChoiceID = correctChoice?.ChoiceID ?? 0,
                IsCorrect = isCorrect,
                Points = question.Points,
                SelectedChoiceText = selectedChoice?.ChoiceText ?? "No answer",
                CorrectChoiceText = correctChoice?.ChoiceText ?? "No correct answer"
            });
        }

        var score = totalPoints > 0 ? (decimal)earnedPoints / totalPoints * 100 : 0;
        var passed = score >= attempt.Quiz.PassingScore;

        return new QuizResult
        {
            AttemptID = attemptId,
            Score = score,
            Passed = passed,
            TotalQuestions = questions.Count,
            CorrectAnswers = correctAnswers,
            TotalPoints = totalPoints,
            EarnedPoints = earnedPoints,
            Duration = attempt.GetDuration() ?? TimeSpan.Zero,
            QuestionResults = questionResults
        };
    }

    public async Task<bool> IsQuizPassedAsync(int userId, int quizId)
    {
        var latestAttempt = await GetLatestAttemptAsync(userId, quizId);
        return latestAttempt?.Passed == true;
    }

    public async Task<int> GetRemainingAttemptsAsync(int userId, int quizId)
    {
        var quiz = await _context.Quizzes.FindAsync(quizId);
        if (quiz == null)
        {
            return 0;
        }

        var attemptCount = await _context.QuizAttempts
            .CountAsync(qa => qa.UserID == userId && qa.QuizID == quizId);

        return Math.Max(0, quiz.MaxAttempts - attemptCount);
    }

    #endregion
}