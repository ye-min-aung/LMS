using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSPlatform.Models;

public class Module
{
    [Key]
    public int ModuleID { get; set; }

    [Required]
    [ForeignKey("Course")]
    public int CourseID { get; set; }

    [Required]
    [StringLength(255)]
    public string Title_EN { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Title_MM { get; set; } = string.Empty;

    [Required]
    public int ModuleOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Course Course { get; set; } = null!;
    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    // Helper methods for localization
    public string GetTitle(string culture = "en") => culture.StartsWith("my") ? Title_MM : Title_EN;
}