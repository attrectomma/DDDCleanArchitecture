using MediatR;

namespace Api5.Application.Common.Interfaces;

/// <summary>
/// Marker interface for CQRS write operations (commands).
/// All commands must implement this interface instead of
/// <see cref="IRequest{TResponse}"/> directly.
/// </summary>
/// <remarks>
/// DESIGN (CQRS): This marker makes the read/write split explicit at the
/// type system level. Compare with API 4 where commands and queries were
/// indistinguishable — both were service method calls.
///
/// The <see cref="Behaviors.TransactionBehavior{TRequest, TResponse}"/>
/// pipeline behavior uses this marker to selectively wrap only commands
/// in an explicit database transaction. Queries (which implement
/// <see cref="IRequest{TResponse}"/> directly) skip the transaction
/// entirely — they are read-only and never call SaveChanges.
///
/// This means:
///   - Command handlers + their domain event handlers run inside one
///     atomic transaction. If anything fails, everything rolls back.
///   - Query handlers run without transaction overhead.
///
/// Without this marker, the TransactionBehavior would have to use
/// naming conventions or folder structure to distinguish commands
/// from queries — a fragile approach. The type constraint is
/// compile-time enforced and self-documenting.
/// </remarks>
/// <typeparam name="TResponse">The type of the response returned by the command handler.</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>;
