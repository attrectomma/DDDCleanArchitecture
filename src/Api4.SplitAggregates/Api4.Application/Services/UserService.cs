using Api4.Application.DTOs.Requests;
using Api4.Application.DTOs.Responses;
using Api4.Application.Exceptions;
using Api4.Application.Interfaces;
using Api4.Domain.UserAggregate;

namespace Api4.Application.Services;

/// <summary>
/// Service responsible for user-related operations.
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 3. The User aggregate is unchanged between
/// API 3 and API 4. Thin orchestrator: factory constructor → repo → UoW.
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
