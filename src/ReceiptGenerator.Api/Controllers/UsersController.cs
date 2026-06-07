using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Interfaces;
using ReceiptGenerator.Domain.Entities;

namespace ReceiptGenerator.Api.Controllers;

[ApiController]
[Authorize(Roles = UserRole.SuperAdmin)]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _userService.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.CreateAsync(request, cancellationToken);

        return result.Status switch
        {
            CreateUserStatus.Created => CreatedAtAction(
                nameof(Get),
                new { id = result.User!.Id },
                result.User),
            CreateUserStatus.UsernameAlreadyExists => Conflict(new
            {
                message = "Este nome de usuário já está cadastrado."
            }),
            CreateUserStatus.InvalidRole => BadRequest(new
            {
                message = "O perfil informado é inválido."
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpPut("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        var activated = await _userService.ActivateAsync(id, cancellationToken);
        return activated ? NoContent() : NotFound();
    }

    [HttpPut("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        if (id == UserId)
        {
            return BadRequest("You cannot deactivate your own user.");
        }

        var deactivated = await _userService.DeactivateAsync(id, cancellationToken);
        return deactivated ? NoContent() : NotFound();
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User id claim was not found."));
}
