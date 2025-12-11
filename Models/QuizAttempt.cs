using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSPlatform.Models;

public class QuizAttempt
{
    [Key]
    public int AttemptID { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserID { get; set; }

    [Required]
    [ForeignKey("Quiz")]
    public int QuizID { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal Score { get; set; } = 0;

    [Required]
    public bool Passed { get; set; } = false;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public string? UserAnswers { get; set; } // JSON string of user's answers

    public int AttemptNumber { get; set; } = 1;

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Quiz Quiz { get; set; } = null!;

    // Helper methods
    public bool IsCompleted => CompletedAt.HasValue;
    public TimeSpan? GetDuration() => CompletedAt?.Subtract(StartedAt);
    
    public void CompleteAttempt(decimal score, bool passed)
    {
        Score = score;
        Passed = passed;
        CompletedAt = DateTime.UtcNow;
    }
}