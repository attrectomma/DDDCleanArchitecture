using Api4.Domain.VoteAggregate;
using FluentAssertions;
using Xunit;

namespace Api4.Domain.UnitTests;

/// <summary>
/// Unit tests for <see cref="Vote"/> aggregate root constructor.
/// </summary>
/// <remarks>
/// DESIGN: In API 4, Vote is its own aggregate root — extracted from the
/// RetroBoard aggregate to eliminate the "aggregate explosion" problem.
/// Casting a vote no longer requires loading the entire retro board or
/// taking a write lock on its xmin. The "one vote per user per note"
/// invariant is now enforced by a DB unique constraint on (NoteId, UserId)
/// plus an application-level check in VoteService, not by the RetroBoard
/// aggregate's in-memory check.
///
/// The Vote constructor is simple (no guard clauses for GUIDs), so this
/// test class has a single test. In API 5, the constructor also raises
/// a <c>VoteCastEvent</c> domain event.
/// </remarks>
public class VoteTests
{
    /// <summary>
    /// Verifies that a Vote created with valid arguments has its properties set correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithValidArgs_SetsProperties()
    {
        // Arrange
        Guid noteId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        // Act
        Vote vote = new Vote(noteId, userId);

        // Assert
        vote.NoteId.Should().Be(noteId);
        vote.UserId.Should().Be(userId);
    }
}
