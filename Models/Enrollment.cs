using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSPlatform.Models;

public class Enrollment
{
    [Key]
    public int EnrollmentID { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserID { get; set; }

    [Required]
    [ForeignKey("Course")]
    public int CourseID { get; set; }

    [Required]
    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = EnrollmentStatus.PendingPayment;

    public DateTime? CompletionDate { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Course Course { get; set; } = null!;
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();

    // Helper methods - computed property, not stored in database
    [NotMapped]
    public bool IsActive => Status == EnrollmentStatus.Approved;
    public bool IsCompleted => Status == EnrollmentStatus.Completed;
    public decimal GetProgressPercentage()
    {
        if (!LessonProgresses.Any()) return 0;
        
        var totalLessons = Course.Modules.SelectMany(m => m.Lessons).Count();
        if (totalLessons == 0) return 0;
        
        var completedLessons = LessonProgresses.Count(lp => lp.IsCompleted);
        return (decimal)completedLessons / totalLessons * 100;
    }
}

public static class EnrollmentStatus
{
    public const string PendingPayment = "Pending Payment";
    public const string Approved = "Approved";
    public const string Completed = "Completed";
}