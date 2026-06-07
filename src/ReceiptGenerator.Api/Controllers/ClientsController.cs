using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Interfaces;

namespace ReceiptGenerator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/clients")]
public sealed class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClientResponse>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _clientService.GetByUserIdAsync(UserId, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClientResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var client = await _clientService.GetByIdAsync(id, UserId, cancellationToken);
        return client is null ? NotFound() : Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponse>> Create(ClientRequest request, CancellationToken cancellationToken)
    {
        var client = await _clientService.CreateAsync(UserId, request, cancellationToken);
        return client is null
            ? BadRequest("Authenticated user was not found.")
            : CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ClientRequest request, CancellationToken cancellationToken)
    {
        var updated = await _clientService.UpdateAsync(id, UserId, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _clientService.DeleteAsync(id, UserId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User id claim was not found."));
}
