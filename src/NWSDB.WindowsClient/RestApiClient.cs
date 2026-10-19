using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NWSDB.Shared.Dtos;

namespace NWSDB.WindowsClient;

/// <summary>Consumes the NWSDB REST services through the API Gateway (JSON over HTTP).</summary>
public class RestApiClient
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri("http://localhost:5000/") };

    public LoginResponse? CurrentUser { get; private set; }

    public async Task<LoginResponse> LoginAsync(string accountNo, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest(accountNo, password));
        await EnsureSuccessAsync(response);
        CurrentUser = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CurrentUser.Token);
        return CurrentUser;
    }

    public async Task<BillDto> GetCurrentBillAsync(string accountNo)
    {
        var response = await _http.GetAsync($"api/bills/{Uri.EscapeDataString(accountNo)}/current");
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<BillDto>())!;
    }

    public async Task<List<BillDto>> GetBillHistoryAsync(string accountNo)
    {
        var response = await _http.GetAsync($"api/bills/{Uri.EscapeDataString(accountNo)}/history");
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<List<BillDto>>()) ?? new List<BillDto>();
    }

    public async Task<PaymentReceiptDto> PayAsync(string accountNo, string channel)
    {
        var response = await _http.PostAsJsonAsync("api/payments", new PaymentRequestDto(accountNo, channel));
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<PaymentReceiptDto>())!;
    }

    /// <summary>Turns an error response into a readable message, e.g. "This bill is already paid."</summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        var message = $"Request failed ({(int)response.StatusCode}).";
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("message", out var m)) message = m.GetString() ?? message;
            else if (json.RootElement.TryGetProperty("title", out var t)) message = t.GetString() ?? message;
        }
        catch (JsonException) { /* body was not JSON */ }
        throw new InvalidOperationException(message);
    }
}
