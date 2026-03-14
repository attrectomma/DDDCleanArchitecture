using Api5.Domain.ProjectAggregate;
using Api5.Domain.RetroAggregate;
using Api5.Domain.UserAggregate;
using Api5.Domain.VoteAggregate;
using Microsoft.EntityFrameworkCore;

namespace Api5.Application.Common.Interfaces;

/// <summary>
/// Read-only database context interface for CQRS query handlers.
/// Exposes <see cref="IQueryable{T}"/> access to entity sets without
/// change tracking or write capabilities.
/// </summary>
/// <remarks>
/// DESIGN (CQRS): This interface is the boundary between the Application
/// layer's query handlers and the Infrastructure layer's DbContext. Query
/// handlers depend on this abstraction — not on the concrete
/// <c>RetroBoardDbContext</c> — keeping the Application layer free of
/// infrastructure concerns.
///
/// The concrete implementation simply delegates to the real DbContext's
/// <c>DbSet</c> properties. All queries through this interface should
/// use <c>AsNoTracking()</c> for optimal read performance.
///
/// Compare with API 4 where read operations went through the same
/// repositories and services as write operations, paying for change
/// tracking overhead on entities that would never be saved.
/// </remarks>
public interface IReadOnlyDbContext
{
    /// <summary>Gets the queryable set of users (read-only).</summary>
    IQueryable<User> Users { get; }

    /// <summary>Gets the queryable set of projects (read-only).</summary>
    IQueryable<Project> Projects { get; }

    /// <summary>Gets the queryable set of project members (read-only).</summary>
    IQueryable<ProjectMember> ProjectMembers { get; }

    /// <summary>Gets the queryable set of retro boards (read-only).</summary>
    IQueryable<RetroBoard> RetroBoards { get; }

    /// <summary>Gets the queryable set of columns (read-only).</summary>
    IQueryable<Column> Columns { get; }

    /// <summary>Gets the queryable set of notes (read-only).</summary>
    IQueryable<Note> Notes { get; }

    /// <summary>Gets the queryable set of votes (read-only).</summary>
    IQueryable<Vote> Votes { get; }
}
