using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSPlatform.Models;

public class Question
{
    [Key]
    public int QuestionID { get; set; }

    [Required]
    [ForeignKey("Quiz")]
    public int QuizID { get; set; }

    [Required]
    public string QuestionText { get; set; } = string.Empty;

    public int QuestionOrder { get; set; } = 1;

    public int Points { get; set; } = 1; // Points awarded for correct answer

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Quiz Quiz { get; set; } = null!;
    public virtual ICollection<AnswerChoice> AnswerChoices { get; set; } = new List<AnswerChoice>();

    // Helper methods
    public AnswerChoice? GetCorrectAnswer() => AnswerChoices.FirstOrDefault(ac => ac.IsCorrect);
    public bool HasCorrectAnswer() => AnswerChoices.Any(ac => ac.IsCorrect);
}