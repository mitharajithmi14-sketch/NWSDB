using Microsoft.EntityFrameworkCore;
using NWSDB.PaymentService.Data;
using NWSDB.PaymentService.Models;
using NWSDB.Shared.Dtos;

namespace NWSDB.PaymentService.Services;

public record PaymentResult(int StatusCode, PaymentReceiptDto? Receipt, string? Error)
{
    public static PaymentResult Ok(PaymentReceiptDto receipt) => new(200, receipt, null);
    public static PaymentResult Fail(int statusCode, string error) => new(statusCode, null, error);
}

public interface IPaymentRepository
{
    Task AddAsync(Payment payment);
    Task<IReadOnlyList<Payment>> GetByAccountAsync(string accountNo);
}

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _db;
    public PaymentRepository(PaymentDbContext db) => _db = db;

    public async Task AddAsync(Payment payment)
    {
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Payment>> GetByAccountAsync(string accountNo) =>
        await _db.Payments.Where(p => p.AccountNo == accountNo)
            .OrderByDescending(p => p.PaidAt).ToListAsync();
}

public interface IPaymentProcessor
{
    Task<PaymentResult> PayAsync(PaymentRequestDto request, string? bearerToken);
}

/// <summary>
/// Business logic for paying a water bill:
/// 1. get the current bill from Billing, 2. reject unknown accounts and paid bills,
/// 3. mark the bill as paid, 4. save the payment with a receipt number, 5. ask Notification to send a receipt.
/// </summary>
public class PaymentProcessor : IPaymentProcessor
{
    public static readonly string[] Channels = { "NWSDB website", "Partner bank app", "Mobile wallet", "Post office" };

    private readonly IBillingClient _billing;
    private readonly INotificationClient _notifications;
    private readonly IPaymentRepository _payments;
    private readonly ILogger<PaymentProcessor> _logger;

    public PaymentProcessor(IBillingClient billing, INotificationClient notifications,
        IPaymentRepository payments, ILogger<PaymentProcessor> logger)
    {
        _billing = billing;
        _notifications = notifications;
        _payments = payments;
        _logger = logger;
    }

    public async Task<PaymentResult> PayAsync(PaymentRequestDto request, string? bearerToken)
    {
        if (!Channels.Contains(request.Channel))
            return PaymentResult.Fail(400, "Choose a valid payment channel.");

        var bill = await _billing.GetCurrentBillAsync(request.AccountNo.Trim());
        if (bill is null)
            return PaymentResult.Fail(404, "Account not found. Check the number on your bill and try again.");
        if (bill.Status == "Paid")
            return PaymentResult.Fail(409, "This bill is already paid.");

        if (!await _billing.MarkPaidAsync(bill.BillId, bearerToken))
            return PaymentResult.Fail(502, "The billing service could not update the bill. Please try again.");

        var payment = new Payment
        {
            ReceiptNo = "NW-" + Random.Shared.Next(10_000_000, 99_999_999),
            AccountNo = bill.AccountNo,
            BillId = bill.BillId,
            Amount = bill.Amount,
            Channel = request.Channel,
            PaidAt = DateTime.UtcNow
        };
        await _payments.AddAsync(payment);

        var receipt = new PaymentReceiptDto(payment.ReceiptNo, payment.AccountNo, payment.BillId,
            payment.Amount, payment.Channel, payment.PaidAt);

        try
        {
            await _notifications.SendReceiptAsync(receipt, bearerToken);
        }
        catch (Exception ex)
        {
            // The payment is already saved, so a failed SMS must not fail the payment.
            _logger.LogWarning(ex, "Receipt {ReceiptNo} saved but the notification could not be sent.", receipt.ReceiptNo);
        }

        return PaymentResult.Ok(receipt);
    }
}
