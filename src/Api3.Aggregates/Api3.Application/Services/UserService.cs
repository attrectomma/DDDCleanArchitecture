using Api3.Application.DTOs.Requests;
using Api3.Application.DTOs.Responses;
using Api3.Application.Exceptions;
using Api3.Application.Interfaces;
using Api3.Domain.UserAggregate;

namespace Api3.Application.Services;

/// <summary>
/// Service responsible for user-related operations.
/// </summary>
/// <remarks>
/// DESIGN: Same thin orchestration as API 1/2. The User aggregate root
/// validates its own construction via the factory constructor. The service
/// simply wires up repository and UoW calls.
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
