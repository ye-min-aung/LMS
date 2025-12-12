using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Pages.Admin.Courses;

[Authorize(Roles = "Admin")]
public class LessonModel : PageModel
{
    private readonly LMSDbContext _context;

    public LessonModel(LMSDbContext context)
    {
        _context = context;
    }

    public Lesson? Lesson { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Lesson = await _context.Lessons
            .Include(l => l.Module)
                .ThenInclude(m => m.Course)
            .Include(l => l.Quiz)
                .ThenInclude(q => q!.Questions)
                    .ThenInclude(q => q.AnswerChoices)
            .Include(l => l.Assignment)
            .FirstOrDefaultAsync(l => l.LessonID == id);

        if (Lesson == null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAddQuizAsync(int lessonId, string title, int passingScore)
    {
        var lesson = await _context.Lessons.FindAsync(lessonId);
        if (lesson == null) return NotFound();

        var quiz = new Quiz
        {
            LessonID = lessonId,
            Title = title,
            PassingScore = passingScore
        };
        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        TempData["Message"] = "Quiz created successfully!";
        return RedirectToPage(new { id = lessonId });
    }

    public async Task<IActionResult> OnPostAddQuestionAsync(int lessonId, int quizId, string questionText, 
        string[] answers, int correctAnswer)
    {
        var quiz = await _context.Quizzes.FindAsync(quizId);
        if (quiz == null) return NotFound();

        var question = new Question
        {
            QuizID = quizId,
            QuestionText = questionText,
            QuestionType = "MultipleChoice"
        };
        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        // Add answer choices
        for (int i = 0; i < answers.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(answers[i]))
            {
                var choice = new AnswerChoice
                {
                    QuestionID = question.QuestionID,
                    ChoiceText = answers[i],
                    IsCorrect = (i == correctAnswer)
                };
                _context.AnswerChoices.Add(choice);
            }
        }
        await _context.SaveChangesAsync();

        TempData["Message"] = "Question added successfully!";
        return RedirectToPage(new { id = lessonId });
    }

    public async Task<IActionResult> OnPostDeleteQuestionAsync(int lessonId, int questionId)
    {
        var question = await _context.Questions
            .Include(q => q.AnswerChoices)
            .FirstOrDefaultAsync(q => q.QuestionID == questionId);
        
        if (question != null)
        {
            _context.AnswerChoices.RemoveRange(question.AnswerChoices);
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Question deleted.";
        }
        return RedirectToPage(new { id = lessonId });
    }

    public async Task<IActionResult> OnPostDeleteQuizAsync(int lessonId, int quizId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.AnswerChoices)
            .FirstOrDefaultAsync(q => q.QuizID == quizId);
        
        if (quiz != null)
        {
            foreach (var question in quiz.Questions)
            {
                _context.AnswerChoices.RemoveRange(question.AnswerChoices);
            }
            _context.Questions.RemoveRange(quiz.Questions);
            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Quiz deleted.";
        }
        return RedirectToPage(new { id = lessonId });
    }

    public async Task<IActionResult> OnPostAddAssignmentAsync(int lessonId, string title, string description, int maxScore)
    {
        var lesson = await _context.Lessons.FindAsync(lessonId);
        if (lesson == null) return NotFound();

        var assignment = new Assignment
        {
            LessonID = lessonId,
            Title = title,
            Description = description,
            MaxScore = maxScore
        };
        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync();

        TempData["Message"] = "Assignment created successfully!";
        return RedirectToPage(new { id = lessonId });
    }

    public async Task<IActionResult> OnPostDeleteAssignmentAsync(int lessonId, int assignmentId)
    {
        var assignment = await _context.Assignments.FindAsync(assignmentId);
        if (assignment != null)
        {
            _context.Assignments.Remove(assignment);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Assignment deleted.";
        }
        return RedirectToPage(new { id = lessonId });
    }
}
