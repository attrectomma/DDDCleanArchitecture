using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using Api5.Domain.UserAggregate;
using MediatR;

namespace Api5.Application.Users.Commands.CreateUser;

/// <summary>
/// Handles the <see cref="CreateUserCommand"/> by creating a User aggregate
/// and persisting it.
/// </summary>
/// <remarks>
/// DESIGN: Each handler is a focused unit of work for a single use case.
/// Compare with API 4's UserService which had both CreateAsync and
/// GetByIdAsync in a single class. Here, each operation has its own handler,
/// making it easier to:
///   - Test in isolation (one handler, one responsibility)
///   - Add cross-cutting concerns via pipeline behaviors
///   - Reason about a single code path
///
/// CQRS: This is a WRITE operation. We use the repository to persist
/// the aggregate and the unit of work to commit changes.
/// </remarks>
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="CreateUserCommandHandler"/>.
    /// </summary>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public CreateUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Creates a new user aggregate and persists it.
    /// </summary>
    /// <param name="request">The create user command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created user response.</returns>
    public async Task<UserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User(request.Name, request.Email);

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserResponse(user.Id, user.Name, user.Email, user.CreatedAt);
    }
}
