using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSPlatform.Models;

public class Lesson
{
    [Key]
    public int LessonID { get; set; }

    [Required]
    [ForeignKey("Module")]
    public int ModuleID { get; set; }

    [Required]
    [StringLength(255)]
    public string Title_EN { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Title_MM { get; set; } = string.Empty;

    [Required]
    public int LessonOrder { get; set; }

    [Required]
    [StringLength(50)]
    public string LessonType { get; set; } = string.Empty;

    [StringLength(512)]
    public string? ContentURL { get; set; }

    public string? Content_EN { get; set; } // For text lessons

    public string? Content_MM { get; set; } // For text lessons

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Module Module { get; set; } = null!;
    public virtual ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
    public virtual Quiz? Quiz { get; set; }
    public virtual Assignment? Assignment { get; set; }

    // Helper methods for localization
    public string GetTitle(string culture = "en") => culture.StartsWith("my") ? Title_MM : Title_EN;
    public string GetContent(string culture = "en") => culture.StartsWith("my") ? Content_MM ?? "" : Content_EN ?? "";
}

public static class LessonTypes
{
    public const string Video = "Video";
    public const string Text = "Text";
    public const string File = "File";
}