using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LMSPlatform.Data;
using LMSPlatform.Models;

namespace LMSPlatform.Services;

public class PaymentService : IPaymentService
{
    private readonly LMSDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _merchantId;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly string _baseUrl;
    private readonly string _returnUrl;
    private readonly string _notifyUrl;
    private readonly bool _isProduction;

    public PaymentService(
        LMSDbContext context,
        IConfiguration configuration,
        ILogger<PaymentService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("KBZPay");
        
        _merchantId = _configuration["KBZPay:MerchantId"] ?? "";
        _apiKey = _configuration["KBZPay:ApiKey"] ?? "";
        _apiSecret = _configuration["KBZPay:ApiSecret"] ?? "";
        _baseUrl = _configuration["KBZPay:BaseUrl"] ?? "https://api.kbzpay.com";
        _returnUrl = _configuration["KBZPay:ReturnUrl"] ?? "";
        _notifyUrl = _configuration["KBZPay:NotifyUrl"] ?? "";
        
        // Check if real credentials are configured
        _isProduction = !string.IsNullOrEmpty(_merchantId) && 
                        _merchantId != "YOUR_MERCHANT_ID" &&
                        !string.IsNullOrEmpty(_apiSecret) &&
                        _apiSecret != "YOUR_API_SECRET";
    }

    public async Task<PaymentInitiationResult> InitiatePaymentAsync(int userId, int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null)
            return new PaymentInitiationResult { Success = false, ErrorMessage = "Course not found" };

        // Check for any existing enrollment
        var existingEnrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId);
        
        if (existingEnrollment != null)
        {
            // Already approved - no need to pay again
            if (existingEnrollment.Status == EnrollmentStatus.Approved)
                return new PaymentInitiationResult { Success = false, ErrorMessage = "Already enrolled" };
            
            // If pending payment, check for existing pending payment and reuse it
            if (existingEnrollment.Status == EnrollmentStatus.PendingPayment)
            {
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.EnrollmentID == existingEnrollment.EnrollmentID && p.Status == PaymentStatusEnum.Pending);
                
                if (existingPayment != null)
                {
                    var existingPaymentUrl = await GeneratePaymentUrlAsync(existingPayment.PaymentID);
                    return new PaymentInitiationResult
                    {
                        Success = true,
                        PaymentId = existingPayment.PaymentID,
                        TransactionId = existingPayment.TransactionID,
                        PaymentUrl = existingPaymentUrl
                    };
                }
            }
            
            // Update existing enrollment to pending payment status
            existingEnrollment.Status = EnrollmentStatus.PendingPayment;
            await _context.SaveChangesAsync();
        }

        // Create new enrollment only if none exists
        var enrollment = existingEnrollment ?? new Enrollment
        {
            UserID = userId,
            CourseID = courseId,
            Status = EnrollmentStatus.PendingPayment
        };
        
        if (existingEnrollment == null)
        {
            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();
        }

        // Create payment record
        var transactionId = GenerateTransactionId();
        var payment = new Payment
        {
            EnrollmentID = enrollment.EnrollmentID,
            Amount = course.Price,
            PaymentMethod = "KBZPay",
            TransactionID = transactionId,
            Status = PaymentStatusEnum.Pending
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var paymentUrl = await GeneratePaymentUrlAsync(payment.PaymentID);

        _logger.LogInformation("Payment initiated: {PaymentId} for User {UserId}, Course {CourseId}", 
            payment.PaymentID, userId, courseId);

        return new PaymentInitiationResult
        {
            Success = true,
            PaymentId = payment.PaymentID,
            TransactionId = transactionId,
            PaymentUrl = paymentUrl
        };
    }

    public async Task<string> GeneratePaymentUrlAsync(int paymentId)
    {
        var payment = await _context.Payments
            .Include(p => p.Enrollment)
                .ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(p => p.PaymentID == paymentId);

        if (payment == null)
            return "";

        // If not production mode, return demo URL
        if (!_isProduction)
        {
            var demoUrl = _configuration["App:BaseUrl"] ?? "https://localhost:7000";
            return $"{demoUrl}/Payment/Process/{paymentId}";
        }

        // Production: Call KBZ Pay API to create payment
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var nonceStr = Guid.NewGuid().ToString("N");
            
            var requestData = new Dictionary<string, object>
            {
                ["merchant_id"] = _merchantId,
                ["timestamp"] = timestamp,
                ["nonce_str"] = nonceStr,
                ["method"] = "kbz.payment.precreate",
                ["biz_content"] = JsonSerializer.Serialize(new
                {
                    merch_order_id = payment.TransactionID,
                    merch_code = _merchantId,
                    appid = _apiKey,
                    trade_type = "WEB",
                    title = payment.Enrollment?.Course?.Title_EN ?? "Course Enrollment",
                    total_amount = ((int)(payment.Amount * 100)).ToString(), // Amount in smallest unit
                    trans_currency = "MMK",
                    callback_url = _returnUrl,
                    notify_url = _notifyUrl
                })
            };

            // Generate signature
            var signString = GenerateSignString(requestData);
            var signature = GenerateSignature(signString);
            requestData["sign"] = signature;
            requestData["sign_type"] = "SHA256";

            var content = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/payment/precreate", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("KBZ Pay Response: {Response}", responseBody);

            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
            
            if (result.TryGetProperty("Response", out var respObj) &&
                respObj.TryGetProperty("code", out var code) &&
                code.GetString() == "0")
            {
                if (respObj.TryGetProperty("pay_url", out var payUrl))
                {
                    return payUrl.GetString() ?? "";
                }
            }

            _logger.LogWarning("KBZ Pay precreate failed: {Response}", responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling KBZ Pay API");
        }

        // Fallback to demo URL if API call fails
        var fallbackUrl = _configuration["App:BaseUrl"] ?? "https://localhost:7000";
        return $"{fallbackUrl}/Payment/Process/{paymentId}";
    }

    private string GenerateSignString(Dictionary<string, object> data)
    {
        var sortedKeys = data.Keys.Where(k => k != "sign" && k != "sign_type")
                                  .OrderBy(k => k);
        var pairs = sortedKeys.Select(k => $"{k}={data[k]}");
        return string.Join("&", pairs);
    }

    private string GenerateSignature(string signString)
    {
        var dataToSign = signString + "&key=" + _apiSecret;
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
        return BitConverter.ToString(hash).Replace("-", "").ToUpper();
    }

    public async Task<Payment?> GetPaymentAsync(int paymentId)
    {
        return await _context.Payments
            .Include(p => p.Enrollment)
                .ThenInclude(e => e.Course)
            .Include(p => p.Enrollment)
                .ThenInclude(e => e.User)
            .FirstOrDefaultAsync(p => p.PaymentID == paymentId);
    }

    public async Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId)
    {
        return await _context.Payments
            .Include(p => p.Enrollment)
            .FirstOrDefaultAsync(p => p.TransactionID == transactionId);
    }

    public async Task<bool> ProcessPaymentCallbackAsync(PaymentCallbackModel callback)
    {
        var payment = await GetPaymentByTransactionIdAsync(callback.TransactionId);
        if (payment == null)
        {
            _logger.LogWarning("Payment callback for unknown transaction: {TransactionId}", callback.TransactionId);
            return false;
        }

        // Verify signature
        if (!await VerifyPaymentSignatureAsync(callback.TransactionId + callback.Amount, callback.Signature))
        {
            _logger.LogWarning("Invalid signature for payment: {PaymentId}", payment.PaymentID);
            return false;
        }

        if (callback.Status == "SUCCESS")
        {
            payment.Status = PaymentStatusEnum.Completed;
            payment.AmountPaid = callback.Amount;
            payment.PaymentDate = DateTime.UtcNow;
            
            await ActivateEnrollmentAsync(payment.PaymentID);
            
            _logger.LogInformation("Payment completed: {PaymentId}", payment.PaymentID);
        }
        else
        {
            payment.Status = PaymentStatusEnum.Failed;
            _logger.LogWarning("Payment failed: {PaymentId}, Error: {Error}", payment.PaymentID, callback.ErrorMessage);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public Task<bool> VerifyPaymentSignatureAsync(string payload, string signature)
    {
        // In demo mode, accept all signatures
        if (!_isProduction)
            return Task.FromResult(true);

        // Production: Verify SHA256 signature from KBZ Pay
        var expectedSignature = GenerateSignature(payload);
        return Task.FromResult(signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<PaymentStatusEnum> GetPaymentStatusAsync(int paymentId)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        return payment?.Status ?? PaymentStatusEnum.Failed;
    }

    public async Task<bool> UpdatePaymentStatusAsync(int paymentId, PaymentStatusEnum status)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null) return false;

        payment.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Payment>> GetUserPaymentsAsync(int userId)
    {
        return await _context.Payments
            .Where(p => p.Enrollment.UserID == userId)
            .Include(p => p.Enrollment)
                .ThenInclude(e => e.Course)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetCoursePaymentsAsync(int courseId)
    {
        return await _context.Payments
            .Where(p => p.Enrollment.CourseID == courseId)
            .Include(p => p.Enrollment)
                .ThenInclude(e => e.User)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ActivateEnrollmentAsync(int paymentId)
    {
        var payment = await _context.Payments
            .Include(p => p.Enrollment)
            .FirstOrDefaultAsync(p => p.PaymentID == paymentId);

        if (payment?.Enrollment == null) return false;

        payment.Enrollment.Status = Models.EnrollmentStatus.Approved;
        payment.Enrollment.EnrollmentDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Enrollment activated: {EnrollmentId}", payment.EnrollmentID);
        return true;
    }

    private string GenerateTransactionId()
    {
        return $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}
