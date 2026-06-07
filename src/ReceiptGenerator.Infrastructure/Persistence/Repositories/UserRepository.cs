using Microsoft.EntityFrameworkCore;
using ReceiptGenerator.Domain.Entities;
using ReceiptGenerator.Domain.Repositories;

namespace ReceiptGenerator.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return _context.Users.SingleOrDefaultAsync(x => x.Username == username.Trim(), cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
