using LMSPlatform.Models;

namespace LMSPlatform.Services;

public interface IQuizService
{
    // Quiz management
    Task<Quiz?> GetQuizByIdAsync(int quizId);
    Task<Quiz> CreateQuizAsync(CreateQuizModel model);
    Task<bool> UpdateQuizAsync(int quizId, UpdateQuizModel model);
    Task<bool> DeleteQuizAsync(int quizId);
    Task<IEnumerable<Quiz>> GetLessonQuizzesAsync(int lessonId);
    
    // Question management
    Task<Question> CreateQuestionAsync(CreateQuestionModel model);
    Task<bool> UpdateQuestionAsync(int questionId, UpdateQuestionModel model);
    Task<bool> DeleteQuestionAsync(int questionId);
    Task<IEnumerable<Question>> GetQuizQuestionsAsync(int quizId);
    
    // Answer choice management
    Task<AnswerChoice> CreateAnswerChoiceAsync(CreateAnswerChoiceModel model);
    Task<bool> UpdateAnswerChoiceAsync(int choiceId, UpdateAnswerChoiceModel model);
    Task<bool> DeleteAnswerChoiceAsync(int choiceId);
    
    // Quiz taking
    Task<QuizAttempt> StartQuizAttemptAsync(int userId, int quizId);
    Task<QuizAttempt> SubmitQuizAttemptAsync(int attemptId, List<QuizAnswerModel> answers);
    Task<QuizAttempt?> GetQuizAttemptAsync(int attemptId);
    Task<IEnumerable<QuizAttempt>> GetUserQuizAttemptsAsync(int userId, int quizId);
    Task<QuizAttempt?> GetLatestAttemptAsync(int userId, int quizId);
    
    // Quiz validation and scoring
    Task<bool> CanUserTakeQuizAsync(int userId, int quizId);
    Task<QuizResult> CalculateQuizScoreAsync(int attemptId);
    Task<bool> IsQuizPassedAsync(int userId, int quizId);
    Task<int> GetRemainingAttemptsAsync(int userId, int quizId);
}

// DTOs for quiz operations
public class CreateQuizModel
{
    public int? LessonID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public bool RequiredToUnlock { get; set; }
    public int PassingScore { get; set; } = 70;
    public int MaxAttempts { get; set; } = 3;
}

public class UpdateQuizModel
{
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public bool RequiredToUnlock { get; set; }
    public int PassingScore { get; set; }
    public int MaxAttempts { get; set; }
}

public class CreateQuestionModel
{
    public int QuizID { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int QuestionOrder { get; set; }
    public int Points { get; set; } = 1;
}

public class UpdateQuestionModel
{
    public string QuestionText { get; set; } = string.Empty;
    public int QuestionOrder { get; set; }
    public int Points { get; set; }
}

public class CreateAnswerChoiceModel
{
    public int QuestionID { get; set; }
    public string ChoiceText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int ChoiceOrder { get; set; }
}

public class UpdateAnswerChoiceModel
{
    public string ChoiceText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int ChoiceOrder { get; set; }
}

public class QuizAnswerModel
{
    public int QuestionID { get; set; }
    public int SelectedChoiceID { get; set; }
}

public class QuizResult
{
    public int AttemptID { get; set; }
    public decimal Score { get; set; }
    public bool Passed { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalPoints { get; set; }
    public int EarnedPoints { get; set; }
    public TimeSpan Duration { get; set; }
    public List<QuestionResult> QuestionResults { get; set; } = new();
}

public class QuestionResult
{
    public int QuestionID { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int SelectedChoiceID { get; set; }
    public int CorrectChoiceID { get; set; }
    public bool IsCorrect { get; set; }
    public int Points { get; set; }
    public string SelectedChoiceText { get; set; } = string.Empty;
    public string CorrectChoiceText { get; set; } = string.Empty;
}