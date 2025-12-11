using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSPlatform.Models;

public class AssignmentSubmission
{
    [Key]
    public int SubmissionID { get; set; }

    [Required]
    [ForeignKey("Assignment")]
    public int AssignmentID { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserID { get; set; }

    [Required]
    [StringLength(512)]
    public string SubmissionFileUrl { get; set; } = string.Empty;

    [StringLength(255)]
    public string? OriginalFileName { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = SubmissionStatus.Pending;

    public string? AdminMark { get; set; } // Optional feedback from admin

    public decimal? Grade { get; set; } // Optional numeric grade

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    [ForeignKey("ReviewedByUser")]
    public int? ReviewedBy { get; set; } // Admin who reviewed

    // Navigation Properties
    public virtual Assignment Assignment { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual User? ReviewedByUser { get; set; }

    // Helper methods
    public bool IsPending => Status == SubmissionStatus.Pending;
    public bool IsPassed => Status == SubmissionStatus.Passed;
    public bool IsFailed => Status == SubmissionStatus.Failed;
    public bool IsReviewed => ReviewedAt.HasValue;
    
    public void MarkAsReviewed(string status, string? feedback, int reviewerId, decimal? grade = null)
    {
        Status = status;
        AdminMark = feedback;
        Grade = grade;
        ReviewedAt = DateTime.UtcNow;
        ReviewedBy = reviewerId;
    }
}

public static class SubmissionStatus
{
    public const string Pending = "Pending";
    public const string Passed = "Passed";
    public const string Failed = "Failed";
}