using System.Runtime.Serialization;

namespace NWSDB.BillingService.Soap;

/// <summary>Bill data sent in SOAP messages (serialised with DataContractSerializer).</summary>
[DataContract(Namespace = SoapNamespaces.Billing)]
public class BillRecord
{
    [DataMember(Order = 1)] public int BillId { get; set; }
    [DataMember(Order = 2)] public string AccountNo { get; set; } = string.Empty;
    [DataMember(Order = 3)] public string Month { get; set; } = string.Empty;      // YYYY-MM
    [DataMember(Order = 4)] public int UnitsUsed { get; set; }
    [DataMember(Order = 5)] public decimal Amount { get; set; }                     // LKR
    [DataMember(Order = 6)] public DateTime DueDate { get; set; }
    [DataMember(Order = 7)] public string Status { get; set; } = "Unpaid";
}

public static class SoapNamespaces
{
    public const string Billing = "http://nwsdb.lk/soap/billing";
}
