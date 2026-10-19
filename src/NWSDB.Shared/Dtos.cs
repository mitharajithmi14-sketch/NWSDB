using System.ComponentModel.DataAnnotations;

namespace NWSDB.Shared.Dtos;

// ---------- Account service ----------
public record LoginRequest(
    [Required(ErrorMessage = "Account number is required.")] string AccountNo,
    [Required(ErrorMessage = "Password is required.")] string Password);

public record LoginResponse(string Token, string AccountNo, string FullName, string Role);

public record RegisterRequest(
    [Required(ErrorMessage = "Account number is required.")] string AccountNo,
    [Required(ErrorMessage = "Full name is required.")] string FullName,
    [Required(ErrorMessage = "Email address is required."), EmailAddress(ErrorMessage = "Enter a valid email address.")] string Email,
    [Required(ErrorMessage = "Phone number is required.")] string Phone,
    [Required(ErrorMessage = "Password is required."), MinLength(6, ErrorMessage = "Password must be at least 6 characters.")] string Password);

public record AccountDto(string AccountNo, string FullName, string Email, string Phone,
    string Address, string ConnectionType, string Status, bool IsRegistered);

// ---------- Billing service ----------
public record BillDto(int BillId, string AccountNo, string Month, int UnitsUsed,
    decimal Amount, DateOnly DueDate, string Status);

public record CreateBillRequest(
    [Required(ErrorMessage = "Account number is required.")] string AccountNo,
    [Required(ErrorMessage = "Month is required."), RegularExpression(@"^\d{4}-\d{2}$", ErrorMessage = "Month must be in the format YYYY-MM.")] string Month,
    [Range(0, 10000, ErrorMessage = "Units used must be between 0 and 10000.")] int UnitsUsed,
    DateOnly DueDate);

public record UpdateBillRequest(
    [Range(0, 1000000, ErrorMessage = "Amount must be between 0 and 1,000,000.")] decimal Amount,
    DateOnly DueDate,
    [Required, RegularExpression("^(Paid|Unpaid)$", ErrorMessage = "Status must be Paid or Unpaid.")] string Status);

// ---------- Usage service ----------
public record UsageDto(int ReadingId, string AccountNo, string Month, int MeterValue, int UnitsUsed);

public record CreateReadingRequest(
    [Required(ErrorMessage = "Account number is required.")] string AccountNo,
    [Required(ErrorMessage = "Month is required."), RegularExpression(@"^\d{4}-\d{2}$", ErrorMessage = "Month must be in the format YYYY-MM.")] string Month,
    [Range(0, int.MaxValue, ErrorMessage = "Meter value cannot be negative.")] int MeterValue,
    [Range(0, 10000, ErrorMessage = "Units used must be between 0 and 10000.")] int UnitsUsed);

// ---------- Payment service ----------
public record PaymentRequestDto(
    [Required(ErrorMessage = "Account number is required.")] string AccountNo,
    [Required(ErrorMessage = "Payment channel is required.")] string Channel);

public record PaymentReceiptDto(string ReceiptNo, string AccountNo, int BillId,
    decimal Amount, string Channel, DateTime PaidAt);

// ---------- Notification service ----------
public record NotificationRequest(
    [Required] string AccountNo,
    [Required] string Channel,
    [Required] string Message);

public record NotificationDto(int NotificationId, string AccountNo, string Channel, string Message, DateTime SentAt);

public record ContactRequest(
    [Required(ErrorMessage = "Name is required.")] string Name,
    [Required(ErrorMessage = "Email address is required."), EmailAddress(ErrorMessage = "Enter a valid email address.")] string Email,
    [Required(ErrorMessage = "Message is required.")] string Message);

public record ContactMessageDto(int Id, string Name, string Email, string Message, DateTime CreatedAt);

// ---------- Common ----------
public record MessageResponse(string Message);
