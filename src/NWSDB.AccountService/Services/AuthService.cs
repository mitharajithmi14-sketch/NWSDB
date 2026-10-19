using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NWSDB.AccountService.Data;
using NWSDB.AccountService.Models;
using NWSDB.Shared.Dtos;

namespace NWSDB.AccountService.Services;

public interface ITokenService
{
    string CreateToken(string accountNo, string fullName, string role);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    public TokenService(IConfiguration config) => _config = config;

    public string CreateToken(string accountNo, string fullName, string role)
    {
        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var claims = new[]
        {
            new Claim("accountNo", accountNo),
            new Claim("name", fullName),
            new Claim("role", role)
        };
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public enum RegisterResult { Success, AccountNotFound, AlreadyRegistered }

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<RegisterResult> RegisterAsync(RegisterRequest request);
}

public class AuthService : IAuthService
{
    private readonly AccountDbContext _db;
    private readonly ITokenService _tokens;
    private readonly PasswordHasher<WaterAccount> _accountHasher = new();
    private readonly PasswordHasher<StaffUser> _staffHasher = new();

    public AuthService(AccountDbContext db, ITokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var id = request.AccountNo.Trim();

        // 1. Customer login with a water account number
        var account = await _db.Accounts.Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.AccountNo == id);
        if (account?.PasswordHash != null &&
            _accountHasher.VerifyHashedPassword(account, account.PasswordHash, request.Password) != PasswordVerificationResult.Failed)
        {
            var name = account.Customer?.FullName ?? id;
            return new LoginResponse(_tokens.CreateToken(id, name, "Customer"), id, name, "Customer");
        }

        // 2. NWSDB staff login with a username
        var staff = await _db.StaffUsers.FirstOrDefaultAsync(s => s.Username == id);
        if (staff != null &&
            _staffHasher.VerifyHashedPassword(staff, staff.PasswordHash, request.Password) != PasswordVerificationResult.Failed)
        {
            return new LoginResponse(_tokens.CreateToken(staff.Username, staff.FullName, staff.Role), staff.Username, staff.FullName, staff.Role);
        }

        return null;
    }

    public async Task<RegisterResult> RegisterAsync(RegisterRequest request)
    {
        var account = await _db.Accounts.Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.AccountNo == request.AccountNo.Trim());
        if (account == null) return RegisterResult.AccountNotFound;
        if (account.PasswordHash != null) return RegisterResult.AlreadyRegistered;

        account.PasswordHash = _accountHasher.HashPassword(account, request.Password);
        if (account.Customer != null)
        {
            account.Customer.FullName = request.FullName.Trim();
            account.Customer.Email = request.Email.Trim();
            account.Customer.Phone = request.Phone.Trim();
        }
        await _db.SaveChangesAsync();
        return RegisterResult.Success;
    }
}
