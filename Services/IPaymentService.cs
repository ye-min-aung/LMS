using LMSPlatform.Models;

namespace LMSPlatform.Services;

public interface IPaymentService
{
    // Payment initiation
    Task<PaymentInitiationResult> InitiatePaymentAsync(int userId, int courseId);
    Task<string> GeneratePaymentUrlAsync(int paymentId);
    
    // Payment processing
    Task<Payment?> GetPaymentAsync(int paymentId);
    Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId);
    Task<bool> ProcessPaymentCallbackAsync(PaymentCallbackModel callback);
    Task<bool> VerifyPaymentSignatureAsync(string payload, string signature);
    
    // Payment status
    Task<PaymentStatusEnum> GetPaymentStatusAsync(int paymentId);
    Task<bool> UpdatePaymentStatusAsync(int paymentId, PaymentStatusEnum status);
    
    // User payments
    Task<IEnumerable<Payment>> GetUserPaymentsAsync(int userId);
    Task<IEnumerable<Payment>> GetCoursePaymentsAsync(int courseId);
    
    // Enrollment
    Task<bool> ActivateEnrollmentAsync(int paymentId);
}

public class PaymentInitiationResult
{
    public bool Success { get; set; }
    public int PaymentId { get; set; }
    public string? PaymentUrl { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PaymentCallbackModel
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Signature { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}
