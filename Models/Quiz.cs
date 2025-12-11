using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSPlatform.Models;

public class Quiz
{
    [Key]
    public int QuizID { get; set; }

    [ForeignKey("Lesson")]
    public int? LessonID { get; set; } // Optional 1:1 relationship

    [StringLength(255)]
    public string? Title { get; set; }

    public string? Instructions { get; set; }

    public bool RequiredToUnlock { get; set; } = false;

    public int PassingScore { get; set; } = 70; // Percentage required to pass

    public int MaxAttempts { get; set; } = 3; // Maximum attempts allowed

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual Lesson? Lesson { get; set; }
    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();

    // Helper methods
    public bool IsPassingScore(decimal score) => score >= PassingScore;
    public int GetTotalQuestions() => Questions.Count;
}