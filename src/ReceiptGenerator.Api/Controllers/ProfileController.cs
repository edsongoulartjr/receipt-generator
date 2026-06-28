using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Interfaces;

namespace ReceiptGenerator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me")]
public sealed class ProfileController : ControllerBase
{
    private readonly IUserService _userService;

    public ProfileController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<UserResponse>> Get(CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(UserId, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateProfileAsync(UserId, request, cancellationToken);

        return result.Status switch
        {
            UpdateProfileStatus.Ok => NoContent(),
            UpdateProfileStatus.UserNotFound => NotFound(),
            UpdateProfileStatus.WrongPassword => BadRequest(new { message = "Senha atual incorreta." }),
            UpdateProfileStatus.NewPasswordRequired => BadRequest(new { message = "Informe a senha atual para definir uma nova senha." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User id claim was not found."));
}
