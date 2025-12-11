using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSPlatform.Models;

public class LessonProgress
{
    [Key]
    public int ProgressID { get; set; }

    [Required]
    [ForeignKey("Enrollment")]
    public int EnrollmentID { get; set; }

    [Required]
    [ForeignKey("Lesson")]
    public int LessonID { get; set; }

    public bool IsCompleted { get; set; } = false;

    public int? VideoTimestamp { get; set; } // Time in seconds for resume playback

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Enrollment Enrollment { get; set; } = null!;
    public virtual Lesson Lesson { get; set; } = null!;

    // Helper methods
    public void MarkCompleted()
    {
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        LastAccessedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(int? timestamp = null)
    {
        if (timestamp.HasValue)
        {
            VideoTimestamp = timestamp.Value;
        }
        LastAccessedAt = DateTime.UtcNow;
        
        if (!StartedAt.HasValue)
        {
            StartedAt = DateTime.UtcNow;
        }
    }
}