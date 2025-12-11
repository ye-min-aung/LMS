using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSPlatform.Models;

public class Course
{
    [Key]
    public int CourseID { get; set; }

    [Required]
    [StringLength(255)]
    public string Title_EN { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Title_MM { get; set; } = string.Empty;

    public string? Description_EN { get; set; }

    public string? Description_MM { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(512)]
    public string? ThumbnailURL { get; set; }

    public bool IsPublished { get; set; } = false;

    // Navigation Properties
    public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    // Helper methods for localization
    public string GetTitle(string culture = "en") => culture.StartsWith("my") ? Title_MM : Title_EN;
    public string GetDescription(string culture = "en") => culture.StartsWith("my") ? Description_MM ?? "" : Description_EN ?? "";
}