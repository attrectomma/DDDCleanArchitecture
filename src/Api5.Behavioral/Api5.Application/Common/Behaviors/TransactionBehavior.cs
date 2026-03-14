using Api5.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Api5.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that wraps command execution in an explicit
/// database transaction, ensuring the handler and all domain event handlers
/// it triggers commit or roll back as a single atomic unit.
/// </summary>
/// <remarks>
/// DESIGN: This behavior solves a subtle but critical problem with domain
/// events. Consider the <c>RemoveNote</c> flow:
///
/// <code>
///   1. RemoveNoteCommandHandler calls SaveChangesAsync()   → note deleted
///   2. DomainEventInterceptor fires NoteRemovedEvent
///   3. NoteRemovedEventHandler deletes orphaned votes
///   4. NoteRemovedEventHandler calls SaveChangesAsync()    → votes deleted
/// </code>
///
/// Without this behavior, steps 1 and 4 are two separate implicit
/// transactions. If step 4 fails (e.g., database timeout), the note is
/// deleted but its votes remain — an inconsistent state.
///
/// With this behavior, both saves happen inside a single explicit
/// transaction. If anything fails, everything rolls back.
///
/// <para><strong>How it coexists with UnitOfWork:</strong></para>
/// They operate at different levels:
/// <list type="bullet">
///   <item><see cref="IUnitOfWork"/> = "flush tracked changes to the database."
///     It calls <c>SaveChangesAsync</c>. It does not manage transactions.</item>
///   <item><see cref="TransactionBehavior{TRequest, TResponse}"/> = "ensure
///     everything the handler does is atomic." It wraps the pipeline in an
///     explicit transaction.</item>
/// </list>
///
/// When an explicit transaction is already open, EF Core's
/// <c>SaveChangesAsync</c> does NOT create its own implicit transaction —
/// it flushes changes within the existing one. So the UnitOfWork continues
/// working exactly as before; it just participates in the transaction this
/// behavior opened.
///
/// <para><strong>Selective activation:</strong></para>
/// The <c>where TRequest : ICommand&lt;TResponse&gt;</c> constraint ensures
/// this behavior only runs for commands. Queries (which implement
/// <see cref="IRequest{TResponse}"/> directly) skip this behavior entirely
/// — they are read-only and never need transaction management.
///
/// <para><strong>Re-entrancy guard:</strong></para>
/// If a transaction is already active (e.g., a domain event handler triggers
/// another command), this behavior skips <c>BeginTransactionAsync</c> and
/// delegates directly. This prevents nested transaction errors and ensures
/// the outermost behavior owns the commit/rollback.
///
/// Order: Runs AFTER <see cref="ValidationBehavior{TRequest, TResponse}"/>
/// (so we never open a transaction for an invalid command) and BEFORE the
/// handler itself.
/// </remarks>
/// <typeparam name="TRequest">The command type (must implement <see cref="ICommand{TResponse}"/>).</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the command handler.</typeparam>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="TransactionBehavior{TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="dbContext">
    /// The EF Core DbContext used to manage the transaction. Injected as
    /// <see cref="DbContext"/> (not the concrete <c>RetroBoardDbContext</c>)
    /// to keep the Application layer free of infrastructure references.
    /// The DI container resolves this to the registered <c>RetroBoardDbContext</c>.
    /// </param>
    /// <param name="logger">The logger instance.</param>
    public TransactionBehavior(
        DbContext dbContext,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Wraps the command handler execution in an explicit database transaction.
    /// If a transaction is already active, delegates directly without nesting.
    /// </summary>
    /// <param name="request">The incoming command.</param>
    /// <param name="next">The next behavior or handler in the pipeline.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response from the handler.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Re-entrancy guard: if a transaction is already active (e.g., a domain
        // event handler dispatching another command), skip and delegate directly.
        // The outermost TransactionBehavior owns the commit/rollback.
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            return await next();
        }

        string commandName = typeof(TRequest).Name;

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        _logger.LogDebug(
            "Began transaction {TransactionId} for {CommandName}",
            transaction.TransactionId,
            commandName);

        try
        {
            TResponse response = await next();

            await transaction.CommitAsync(cancellationToken);

            _logger.LogDebug(
                "Committed transaction {TransactionId} for {CommandName}",
                transaction.TransactionId,
                commandName);

            return response;
        }
        catch
        {
            _logger.LogWarning(
                "Rolling back transaction {TransactionId} for {CommandName}",
                transaction.TransactionId,
                commandName);

            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
