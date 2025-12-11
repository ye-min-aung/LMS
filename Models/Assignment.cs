using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSPlatform.Models;

public class Assignment
{
    [Key]
    public int AssignmentID { get; set; }

    [ForeignKey("Lesson")]
    public int? LessonID { get; set; } // Optional 1:1 relationship

    [StringLength(255)]
    public string? Title { get; set; }

    public string? Instructions { get; set; }

    public DateTime? DueDate { get; set; }

    public int MaxFileSize { get; set; } = 10485760; // 10MB in bytes

    [StringLength(255)]
    public string AllowedFileTypes { get; set; } = "pdf,doc,docx,txt,zip"; // Comma-separated

    public bool IsRequired { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual Lesson? Lesson { get; set; }
    public virtual ICollection<AssignmentSubmission> AssignmentSubmissions { get; set; } = new List<AssignmentSubmission>();

    // Helper methods
    public bool IsOverdue() => DueDate.HasValue && DateTime.UtcNow > DueDate.Value;
    public string[] GetAllowedFileTypesArray() => AllowedFileTypes.Split(',', StringSplitOptions.RemoveEmptyEntries);
    public bool IsFileTypeAllowed(string fileExtension)
    {
        var allowedTypes = GetAllowedFileTypesArray();
        return allowedTypes.Contains(fileExtension.TrimStart('.').ToLower());
    }
}