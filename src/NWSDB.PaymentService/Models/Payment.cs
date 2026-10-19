namespace NWSDB.PaymentService.Models;

public class Payment
{
    public int PaymentId { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public string AccountNo { get; set; } = string.Empty;
    public int BillId { get; set; }
    public decimal Amount { get; set; }
    /// <summary>NWSDB website, Partner bank app, Mobile wallet or Post office.</summary>
    public string Channel { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
}
