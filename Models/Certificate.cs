using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSPlatform.Models;

public class Certificate
{
    [Key]
    public int CertificateID { get; set; }

    [Required]
    [ForeignKey("User")]
    public int UserID { get; set; }

    [Required]
    [ForeignKey("Course")]
    public int CourseID { get; set; }

    [Required]
    public DateTime DateIssued { get; set; } = DateTime.UtcNow;

    public DateTime IssuedDate 
    { 
        get => DateIssued; 
        set => DateIssued = value; 
    }

    public DateTime CompletionDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(5,2)")]
    public decimal FinalGrade { get; set; } = 0;

    [Required]
    [StringLength(50)]
    public string UniqueCertID { get; set; } = string.Empty;

    [StringLength(512)]
    public string? CertificateFileUrl { get; set; } // Path to generated PDF

    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual Course Course { get; set; } = null!;

    // Helper methods
    public string GenerateUniqueCertID()
    {
        return $"CERT-{CourseID:D4}-{UserID:D6}-{DateIssued:yyyyMMdd}";
    }

    public string GetCertificateFileName()
    {
        return $"certificate_{UniqueCertID}.pdf";
    }
}