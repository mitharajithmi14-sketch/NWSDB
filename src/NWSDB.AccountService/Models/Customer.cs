namespace NWSDB.AccountService.Models;

public class Customer
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public List<WaterAccount> Accounts { get; set; } = new();
}

public class WaterAccount
{
    public string AccountNo { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Null until the customer registers for online services.</summary>
    public string? PasswordHash { get; set; }
    public string ConnectionType { get; set; } = "Domestic";
    public string Status { get; set; } = "Active";
}

public class StaffUser
{
    public int StaffUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
}
