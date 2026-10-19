using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace NWSDB.WindowsClient;

/// <summary>Bill data returned by the SOAP service.</summary>
public class SoapBill
{
    public int BillId { get; set; }
    public string AccountNo { get; set; } = "";
    public string Month { get; set; } = "";
    public int UnitsUsed { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "Unpaid";
}

/// <summary>
/// Consumes the Billing SOAP web service: builds SOAP 1.1 envelopes, posts them with a SOAPAction
/// header and reads the XML response. WSDL: http://localhost:5002/soap/BillingService.asmx?wsdl
/// </summary>
public class SoapBillingClient
{
    private const string Url = "http://localhost:5002/soap/BillingService.asmx";
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace Ns = "http://nwsdb.lk/soap/billing";
    private readonly HttpClient _http = new();

    public async Task<List<SoapBill>> GetBillsAsync(string accountNo)
    {
        var body = await CallAsync("GetBills", new XElement(Ns + "accountNo", accountNo));
        return body.Descendants().Where(e => e.Name.LocalName == "BillRecord").Select(ReadBill).ToList();
    }

    public async Task<int> CreateBillAsync(SoapBill bill) =>
        int.Parse(Result(await CallAsync("CreateBill", BillElement(bill)), "CreateBillResult"));

    public async Task<bool> UpdateBillAsync(SoapBill bill) =>
        bool.Parse(Result(await CallAsync("UpdateBill", BillElement(bill)), "UpdateBillResult"));

    public async Task<bool> DeleteBillAsync(int billId) =>
        bool.Parse(Result(await CallAsync("DeleteBill", new XElement(Ns + "billId", billId)), "DeleteBillResult"));

    private async Task<XElement> CallAsync(string operation, XElement parameter)
    {
        var envelope = new XDocument(
            new XElement(Soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", Soap),
                new XElement(Soap + "Body", new XElement(Ns + operation, parameter))));

        using var request = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "text/xml")
        };
        request.Headers.Add("SOAPAction", $"\"{Ns.NamespaceName}/IBillingSoapService/{operation}\"");

        var response = await _http.SendAsync(request);
        var xml = XDocument.Parse(await response.Content.ReadAsStringAsync());
        var fault = xml.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
        if (fault != null)
        {
            var text = fault.Descendants().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value;
            throw new InvalidOperationException(text ?? "The SOAP service returned a fault.");
        }
        return xml.Descendants(Soap + "Body").First();
    }

    private static XElement BillElement(SoapBill b) =>
        new(Ns + "bill",                                   // element order matches [DataMember(Order = ...)]
            new XElement(Ns + "BillId", b.BillId),
            new XElement(Ns + "AccountNo", b.AccountNo),
            new XElement(Ns + "Month", b.Month),
            new XElement(Ns + "UnitsUsed", b.UnitsUsed),
            new XElement(Ns + "Amount", XmlConvert.ToString(b.Amount)),
            new XElement(Ns + "DueDate", XmlConvert.ToString(b.DueDate.Date, XmlDateTimeSerializationMode.Unspecified)),
            new XElement(Ns + "Status", b.Status));

    private static SoapBill ReadBill(XElement e)
    {
        string V(string name) => e.Elements().First(x => x.Name.LocalName == name).Value;
        return new SoapBill
        {
            BillId = int.Parse(V("BillId")),
            AccountNo = V("AccountNo"),
            Month = V("Month"),
            UnitsUsed = int.Parse(V("UnitsUsed")),
            Amount = decimal.Parse(V("Amount"), CultureInfo.InvariantCulture),
            DueDate = XmlConvert.ToDateTime(V("DueDate"), XmlDateTimeSerializationMode.Unspecified),
            Status = V("Status")
        };
    }

    private static string Result(XElement body, string name) =>
        body.Descendants().First(e => e.Name.LocalName == name).Value;
}
