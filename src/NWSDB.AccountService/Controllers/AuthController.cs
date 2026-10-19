using Microsoft.AspNetCore.Mvc;
using NWSDB.AccountService.Services;
using NWSDB.Shared.Dtos;

namespace NWSDB.AccountService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Checks the account number (or staff username) and password and returns a JWT token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(MessageResponse), 401)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _auth.LoginAsync(request);
        return result is null
            ? Unauthorized(new MessageResponse("Account number or password is incorrect. Try again."))
            : Ok(result);
    }

    /// <summary>Registers an existing water account for online services.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(MessageResponse), 200)]
    [ProducesResponseType(typeof(MessageResponse), 404)]
    [ProducesResponseType(typeof(MessageResponse), 409)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        return (await _auth.RegisterAsync(request)) switch
        {
            RegisterResult.AccountNotFound => NotFound(new MessageResponse("Account not found. Check the number on your bill and try again.")),
            RegisterResult.AlreadyRegistered => Conflict(new MessageResponse("This account is already registered. Please log in.")),
            _ => Ok(new MessageResponse("Registration successful. You can now log in."))
        };
    }
}
