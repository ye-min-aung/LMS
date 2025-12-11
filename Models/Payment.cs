using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LMSPlatform.Models;

public class Payment
{
    [Key]
    public int PaymentID { get; set; }

    [Required]
    [ForeignKey("Enrollment")]
    public int EnrollmentID { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal AmountPaid { get; set; }

    public DateTime? PaymentDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    public string PaymentMethod { get; set; } = "KBZPay";

    [StringLength(100)]
    public string? TransactionID { get; set; }

    public PaymentStatusEnum Status { get; set; } = PaymentStatusEnum.Pending;

    [StringLength(512)]
    public string? ProofURL { get; set; }

    public string? PaymentResponse { get; set; }

    [StringLength(50)]
    public string AdminApprovalStatus { get; set; } = "Pending";

    // Navigation Properties
    public virtual Enrollment Enrollment { get; set; } = null!;

    // Helper methods
    public bool IsCompleted => Status == PaymentStatusEnum.Completed;
}

public enum PaymentStatusEnum
{
    Pending,
    Approved,
    Completed,
    Failed,
    Refunded
}

public static class PaymentStatusStrings
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}