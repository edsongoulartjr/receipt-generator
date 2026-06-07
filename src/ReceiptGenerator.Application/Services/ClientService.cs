using ReceiptGenerator.Application.DTOs;
using ReceiptGenerator.Application.Interfaces;
using ReceiptGenerator.Domain.Entities;
using ReceiptGenerator.Domain.Repositories;

namespace ReceiptGenerator.Application.Services;

public sealed class ClientService : IClientService
{
    private readonly IClientRepository _clients;
    private readonly IUserRepository _users;

    public ClientService(IClientRepository clients, IUserRepository users)
    {
        _clients = clients;
        _users = users;
    }

    public async Task<IReadOnlyList<ClientResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var clients = await _clients.GetByUserIdAsync(userId, cancellationToken);
        return clients.Select(Map).ToList();
    }

    public async Task<ClientResponse?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var client = await _clients.GetByIdAndUserIdAsync(id, userId, cancellationToken);
        return client is null ? null : Map(client);
    }

    public async Task<ClientResponse?> CreateAsync(int userId, ClientRequest request, CancellationToken cancellationToken = default)
    {
        if (await _users.GetByIdAsync(userId, cancellationToken) is null)
        {
            return null;
        }

        var client = new Client(request.Name, request.Address, request.TaxId, userId);
        await _clients.AddAsync(client, cancellationToken);
        return Map(client);
    }

    public async Task<bool> UpdateAsync(int id, int userId, ClientRequest request, CancellationToken cancellationToken = default)
    {
        var client = await _clients.GetByIdAndUserIdAsync(id, userId, cancellationToken);
        if (client is null)
        {
            return false;
        }

        client.Update(request.Name, request.Address, request.TaxId);
        await _clients.UpdateAsync(client, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var client = await _clients.GetByIdAndUserIdAsync(id, userId, cancellationToken);
        if (client is null)
        {
            return false;
        }

        await _clients.DeleteAsync(client, cancellationToken);
        return true;
    }

    private static ClientResponse Map(Client client) => new(client.Id, client.Name, client.Address, client.TaxId);
}
