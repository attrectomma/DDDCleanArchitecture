using Api2.Application.DTOs.Requests;
using Api2.Application.DTOs.Responses;
using Api2.Application.Exceptions;
using Api2.Application.Interfaces;
using Api2.Domain.Entities;

namespace Api2.Application.Services;

/// <summary>
/// Service responsible for user-related operations.
/// </summary>
/// <remarks>
/// DESIGN: In API 2 the User entity uses a factory constructor instead
/// of public setters. The service creates a User via <c>new User(name, email)</c>
/// rather than object-initializer syntax. This is the simplest example
/// of the rich-domain shift — the entity validates its own construction.
/// Compare with API 1 where User was a plain DTO.
/// </remarks>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="UserService"/>.
    /// </summary>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        // DESIGN: Factory constructor validates name and email — no more raw property assignment.
        var user = new User(request.Name, request.Email);

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt);
    }

    /// <inheritdoc />
    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        User user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("User", id);

        return new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt);
    }
}
