using System.ServiceModel;
using System.Text.RegularExpressions;
using NWSDB.BillingService.Data;
using NWSDB.BillingService.Services;

namespace NWSDB.BillingService.Soap;

/// <summary>SOAP contract for NWSDB staff and partner systems. WSDL: /soap/BillingService.asmx?wsdl</summary>
[ServiceContract(Namespace = SoapNamespaces.Billing)]
public interface IBillingSoapService
{
    [OperationContract] List<BillRecord> GetBills(string accountNo);
    [OperationContract] BillRecord? GetBill(int billId);
    [OperationContract] int CreateBill(BillRecord bill);
    [OperationContract] bool UpdateBill(BillRecord bill);
    [OperationContract] bool DeleteBill(int billId);
}

public class BillingSoapService : IBillingSoapService
{
    private readonly BillSqlRepository _repo;
    private readonly ITariffCalculator _tariff;

    public BillingSoapService(BillSqlRepository repo, ITariffCalculator tariff)
    {
        _repo = repo;
        _tariff = tariff;
    }

    public List<BillRecord> GetBills(string accountNo)
    {
        if (string.IsNullOrWhiteSpace(accountNo)) throw new FaultException("Account number is required.");
        return _repo.GetByAccount(accountNo.Trim());
    }

    public BillRecord? GetBill(int billId) => _repo.GetById(billId);

    public int CreateBill(BillRecord bill)
    {
        Validate(bill);
        bill.Amount = _tariff.Calculate(bill.UnitsUsed);   // amount always comes from the tariff
        bill.Status = "Unpaid";
        try
        {
            return _repo.Create(bill);
        }
        catch (System.Data.SqlClient.SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new FaultException($"A bill for {bill.AccountNo} in {bill.Month} already exists.");
        }
    }

    public bool UpdateBill(BillRecord bill)
    {
        Validate(bill);
        if (bill.Status is not ("Paid" or "Unpaid")) throw new FaultException("Status must be Paid or Unpaid.");
        return _repo.Update(bill);
    }

    public bool DeleteBill(int billId) => _repo.Delete(billId);

    private static void Validate(BillRecord bill)
    {
        if (string.IsNullOrWhiteSpace(bill.AccountNo)) throw new FaultException("Account number is required.");
        if (!Regex.IsMatch(bill.Month ?? "", @"^\d{4}-\d{2}$")) throw new FaultException("Month must be in the format YYYY-MM.");
        if (bill.UnitsUsed is < 0 or > 10000) throw new FaultException("Units used must be between 0 and 10000.");
    }
}
