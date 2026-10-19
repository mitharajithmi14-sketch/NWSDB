namespace NWSDB.BillingService.Models;

public class Bill
{
    public int BillId { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    /// <summary>Billing month in the format YYYY-MM.</summary>
    public string Month { get; set; } = string.Empty;
    public int UnitsUsed { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = BillStatus.Unpaid;
}

public static class BillStatus
{
    public const string Paid = "Paid";
    public const string Unpaid = "Unpaid";
}
