using Api5.Domain.UserAggregate;
using Microsoft.EntityFrameworkCore;

namespace Api5.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/>.
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 3/4. In API 5, this repository is used
/// only by the <c>CreateUserCommandHandler</c> (write side). The
/// <c>GetUserQueryHandler</c> uses <c>IReadOnlyDbContext</c> instead.
/// </remarks>
public class UserRepository : IUserRepository
{
    private readonly RetroBoardDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="UserRepository"/>.
    /// </summary>
    /// <param name="context">The EF Core DbContext.</param>
    public UserRepository(RetroBoardDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await _context.Users.AddAsync(user, cancellationToken);
}
