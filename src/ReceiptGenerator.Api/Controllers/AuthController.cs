using Microsoft.AspNetCore.Mvc;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Interfaces;

namespace ReceiptGenerator.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var created = await _authService.RegisterAsync(request, cancellationToken);
        return created ? Created("api/auth/register", null) : Conflict("Username already exists.");
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return response is null ? Unauthorized("Invalid credentials.") : Ok(response);
    }
}
