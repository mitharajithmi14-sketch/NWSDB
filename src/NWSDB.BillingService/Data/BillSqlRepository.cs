using System.Data;
using System.Data.SqlClient;
using NWSDB.BillingService.Soap;

namespace NWSDB.BillingService.Data;

/// <summary>
/// ADO.NET data access with System.Data.SqlClient: Create, Read, Update and Delete on the Bills table.
/// Every value is sent as a SqlParameter, which prevents SQL injection.
/// </summary>
public class BillSqlRepository
{
    private const string Columns = "BillId, AccountNo, Month, UnitsUsed, Amount, DueDate, Status";
    private readonly string _connectionString;

    public BillSqlRepository(IConfiguration config) =>
        _connectionString = config.GetConnectionString("BillingDb")
            ?? throw new InvalidOperationException("Connection string 'BillingDb' is missing.");

    // CREATE - returns the new BillId
    public int Create(BillRecord bill)
    {
        const string sql = @"INSERT INTO Bills (AccountNo, Month, UnitsUsed, Amount, DueDate, Status)
                             OUTPUT INSERTED.BillId
                             VALUES (@AccountNo, @Month, @UnitsUsed, @Amount, @DueDate, @Status)";
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        AddParameters(command, bill);
        connection.Open();
        return (int)command.ExecuteScalar();
    }

    // READ - one bill
    public BillRecord? GetById(int billId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand($"SELECT {Columns} FROM Bills WHERE BillId = @BillId", connection);
        command.Parameters.Add("@BillId", SqlDbType.Int).Value = billId;
        connection.Open();
        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    // READ - all bills of an account
    public List<BillRecord> GetByAccount(string accountNo)
    {
        var bills = new List<BillRecord>();
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            $"SELECT {Columns} FROM Bills WHERE AccountNo = @AccountNo ORDER BY Month", connection);
        command.Parameters.Add("@AccountNo", SqlDbType.NVarChar, 20).Value = accountNo;
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read()) bills.Add(Map(reader));
        return bills;
    }

    // UPDATE - true when a row was changed
    public bool Update(BillRecord bill)
    {
        const string sql = @"UPDATE Bills SET AccountNo = @AccountNo, Month = @Month, UnitsUsed = @UnitsUsed,
                             Amount = @Amount, DueDate = @DueDate, Status = @Status WHERE BillId = @BillId";
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        AddParameters(command, bill);
        command.Parameters.Add("@BillId", SqlDbType.Int).Value = bill.BillId;
        connection.Open();
        return command.ExecuteNonQuery() == 1;
    }

    // DELETE - true when a row was removed
    public bool Delete(int billId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand("DELETE FROM Bills WHERE BillId = @BillId", connection);
        command.Parameters.Add("@BillId", SqlDbType.Int).Value = billId;
        connection.Open();
        return command.ExecuteNonQuery() == 1;
    }

    private static void AddParameters(SqlCommand command, BillRecord bill)
    {
        command.Parameters.Add("@AccountNo", SqlDbType.NVarChar, 20).Value = bill.AccountNo;
        command.Parameters.Add("@Month", SqlDbType.NVarChar, 7).Value = bill.Month;
        command.Parameters.Add("@UnitsUsed", SqlDbType.Int).Value = bill.UnitsUsed;
        var amount = command.Parameters.Add("@Amount", SqlDbType.Decimal);
        amount.Precision = 18;
        amount.Scale = 2;
        amount.Value = bill.Amount;
        command.Parameters.Add("@DueDate", SqlDbType.Date).Value = bill.DueDate.Date;
        command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = bill.Status;
    }

    private static BillRecord Map(SqlDataReader r) => new()
    {
        BillId = r.GetInt32(0),
        AccountNo = r.GetString(1),
        Month = r.GetString(2),
        UnitsUsed = r.GetInt32(3),
        Amount = r.GetDecimal(4),
        DueDate = r.GetDateTime(5),
        Status = r.GetString(6)
    };
}
