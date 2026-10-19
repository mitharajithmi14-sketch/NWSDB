using Microsoft.Extensions.Logging.Abstractions;
using NWSDB.PaymentService.Models;
using NWSDB.PaymentService.Services;
using NWSDB.Shared.Dtos;
using Xunit;

namespace NWSDB.Tests;

public class PaymentProcessorTests
{
    // ---------- simple fakes, so the Payment logic is tested without the other services ----------
    private class FakeBillingClient : IBillingClient
    {
        public BillDto? Bill { get; set; }
        public bool MarkPaidCalled { get; private set; }
        public Task<BillDto?> GetCurrentBillAsync(string accountNo) => Task.FromResult(Bill);
        public Task<bool> MarkPaidAsync(int billId, string? bearerToken) { MarkPaidCalled = true; return Task.FromResult(true); }
    }

    private class FakeNotificationClient : INotificationClient
    {
        public bool Fail { get; set; }
        public PaymentReceiptDto? Sent { get; private set; }
        public Task SendReceiptAsync(PaymentReceiptDto receipt, string? bearerToken)
        {
            if (Fail) throw new HttpRequestException("SMS gateway down");
            Sent = receipt;
            return Task.CompletedTask;
        }
    }

    private class FakePaymentRepository : IPaymentRepository
    {
        public List<Payment> Saved { get; } = new();
        public Task AddAsync(Payment payment) { Saved.Add(payment); return Task.CompletedTask; }
        public Task<IReadOnlyList<Payment>> GetByAccountAsync(string accountNo) =>
            Task.FromResult<IReadOnlyList<Payment>>(Saved.Where(p => p.AccountNo == accountNo).ToList());
    }

    private static BillDto UnpaidBill => new(6, "1002", "2026-09", 9, 840m, new DateOnly(2026, 10, 20), "Unpaid");

    private static (PaymentProcessor, FakeBillingClient, FakeNotificationClient, FakePaymentRepository) Create(BillDto? bill)
    {
        var billing = new FakeBillingClient { Bill = bill };
        var notify = new FakeNotificationClient();
        var repo = new FakePaymentRepository();
        return (new PaymentProcessor(billing, notify, repo, NullLogger<PaymentProcessor>.Instance), billing, notify, repo);
    }

    [Fact]
    public async Task Pay_UnpaidBill_SavesPaymentAndSendsReceipt()
    {
        var (processor, billing, notify, repo) = Create(UnpaidBill);

        var result = await processor.PayAsync(new PaymentRequestDto("1002", "Mobile wallet"), "Bearer test");

        Assert.Equal(200, result.StatusCode);
        Assert.StartsWith("NW-", result.Receipt!.ReceiptNo);
        Assert.Equal(840m, result.Receipt.Amount);
        Assert.True(billing.MarkPaidCalled);
        Assert.Single(repo.Saved);
        Assert.Equal(result.Receipt.ReceiptNo, notify.Sent!.ReceiptNo);
    }

    [Fact]
    public async Task Pay_AlreadyPaidBill_Returns409()
    {
        var (processor, billing, _, repo) = Create(UnpaidBill with { Status = "Paid" });

        var result = await processor.PayAsync(new PaymentRequestDto("1002", "NWSDB website"), null);

        Assert.Equal(409, result.StatusCode);
        Assert.Equal("This bill is already paid.", result.Error);
        Assert.False(billing.MarkPaidCalled);
        Assert.Empty(repo.Saved);
    }

    [Fact]
    public async Task Pay_UnknownAccount_Returns404()
    {
        var (processor, _, _, _) = Create(null);
        var result = await processor.PayAsync(new PaymentRequestDto("9999", "NWSDB website"), null);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task Pay_InvalidChannel_Returns400()
    {
        var (processor, _, _, _) = Create(UnpaidBill);
        var result = await processor.PayAsync(new PaymentRequestDto("1002", "Cash in hand"), null);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Pay_NotificationFails_PaymentStillSucceeds()
    {
        var (processor, _, notify, repo) = Create(UnpaidBill);
        notify.Fail = true;

        var result = await processor.PayAsync(new PaymentRequestDto("1002", "Post office"), null);

        Assert.Equal(200, result.StatusCode);
        Assert.Single(repo.Saved);
    }
}
