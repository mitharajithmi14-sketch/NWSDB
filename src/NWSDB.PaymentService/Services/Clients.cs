using System.Net;
using System.Net.Http.Json;
using NWSDB.Shared.Dtos;

namespace NWSDB.PaymentService.Services;

/// <summary>Calls the Billing Service over HTTP.</summary>
public interface IBillingClient
{
    Task<BillDto?> GetCurrentBillAsync(string accountNo);
    Task<bool> MarkPaidAsync(int billId, string? bearerToken);
}

/// <summary>Calls the Notification Service over HTTP.</summary>
public interface INotificationClient
{
    Task SendReceiptAsync(PaymentReceiptDto receipt, string? bearerToken);
}

public class BillingClient : IBillingClient
{
    private readonly HttpClient _http;
    public BillingClient(HttpClient http) => _http = http;

    public async Task<BillDto?> GetCurrentBillAsync(string accountNo)
    {
        var response = await _http.GetAsync($"api/bills/{Uri.EscapeDataString(accountNo)}/current");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BillDto>();
    }

    public async Task<bool> MarkPaidAsync(int billId, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/bills/{billId}/pay");
        if (bearerToken != null) request.Headers.TryAddWithoutValidation("Authorization", bearerToken);
        var response = await _http.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
}

public class NotificationClient : INotificationClient
{
    private readonly HttpClient _http;
    public NotificationClient(HttpClient http) => _http = http;

    public async Task SendReceiptAsync(PaymentReceiptDto receipt, string? bearerToken)
    {
        var message = $"NWSDB: Payment of LKR {receipt.Amount:N2} for account {receipt.AccountNo} received via {receipt.Channel}. Receipt {receipt.ReceiptNo}.";
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/notifications")
        {
            Content = JsonContent.Create(new NotificationRequest(receipt.AccountNo, "SMS", message))
        };
        if (bearerToken != null) request.Headers.TryAddWithoutValidation("Authorization", bearerToken);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
